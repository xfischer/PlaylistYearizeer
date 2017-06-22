using E.Deezer;
using E.Deezer.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        Deezer _deezer;
        IPlaylist _playlist;
        List<IPlaylist> _playlists;

        public Form1()
        {
            InitializeComponent();

            this.FormClosing += (o, e) => { if (_deezer != null) _deezer.Logout(); };
        }


        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                _deezer = DeezerSession.CreateNew();

                await _deezer.Login(txtToken.Text);

                AddToLog("Logged in.");

                panel1.Enabled = true;
            }
            catch (Exception v_ex)
            {
                MessageBox.Show(v_ex.ToString());
            }
        }

        private async void btnFindPlaylist_Click(object sender, EventArgs e)
        {
            try
            {
                _playlists = (await _deezer.User.Current.GetPlaylists(250)).ToList();
                var test = _playlists.FirstOrDefault(p => p.IsLovedTrack);

                _playlist = _playlists.FirstOrDefault(p => p.Title.ToLower() == txtPlaylist.Text.ToLower());

                if (_playlist == null)
                {
                    AddToLog("Playlist not found !");
                }
                else
                {
                    AddToLog("Playlist found.");
                    txtPlaylist.Text = _playlist.Title;
                }


            }
            catch (Exception v_ex)
            {
                MessageBox.Show(v_ex.ToString());
            }


        }

        private void AddToLog(string logString)
        {
            txtReport.AppendText(logString);
            txtReport.AppendText(Environment.NewLine);
        }

        private async void btnReport_Click(object sender, EventArgs e)
        {
            try
            {

                var tracks = await _playlist.GetTracks(_playlist.NumTracks);


                var tracksByYear = from track in tracks
                                   group track by track.TimeAdd.Year into g
                                   select new PlaylistReport
                                   {
                                       Year = g.Key // year
                                               ,
                                       PlaylistTitleWithYear = _playlist.Title.TrimEnd() + " " + g.Key.ToString() // playlist + year
                                               ,
                                       PlaylistId = 0
                                               ,
                                       TrackIds = g.Select(t => t.Id).ToList()
                                   }; // list of track ids for easier add
                List<PlaylistReport> playlistsReport = tracksByYear.OrderBy(r => r.Year).ToList();

                string report = null;
                foreach (var groups in playlistsReport)
                {
                    report = string.Concat(report, groups.Year, ": ", groups.TrackIds.Count, " song(s).", Environment.NewLine);
                }
                AddToLog(report);

                // Get or create playlist
                foreach (PlaylistReport playlistReport in playlistsReport)
                {
                    IPlaylist playlist = _playlists.FirstOrDefault(p => p.Title == playlistReport.PlaylistTitleWithYear);
                    if (playlist != null)
                    {
                        AddToLog("Playlist <" + playlistReport.PlaylistTitleWithYear + "> exists");
                        playlistReport.PlaylistId = playlist.Id;
                    }
                    else
                    {
                        // create playlist
                        AddToLog("Create playlist <" + playlistReport.PlaylistTitleWithYear + ">");
                        uint id = await _deezer.User.Current.CreatePlaylist(playlistReport.PlaylistTitleWithYear);
                        playlistReport.PlaylistId = id;
                    }
                }

                // Add songs to playlist by year
                foreach (PlaylistReport playlistReport in playlistsReport)
                {
                    AddToLog("Adding " + playlistReport.TrackIds.Count.ToString() + " song(s) to playlist <" + playlistReport.PlaylistTitleWithYear + ">");
                    int skip = 0;
                    var nextSongIds = playlistReport.TrackIds.Skip(skip).Take(10);
                    while (nextSongIds.Any())
                    {
                        string songIdsQuery = string.Join<long>(",", nextSongIds);

                        await _deezer.User.Current.AddToPlaylist(playlistReport.PlaylistId, songIdsQuery);
                        skip += 10;
                        nextSongIds = playlistReport.TrackIds.Skip(skip).Take(10);
                    }
                }

                AddToLog("Done !");


            }
            catch (Exception v_ex)
            {
                MessageBox.Show(v_ex.ToString());
            }
        }
    }

    internal class PlaylistReport
    {
        public uint PlaylistId { get; set; }
        public string PlaylistTitleWithYear { get; set; }
        public List<long> TrackIds { get; set; }
        public int Year { get; set; }
    }
}
