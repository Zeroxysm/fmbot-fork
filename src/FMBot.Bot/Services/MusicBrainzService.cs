using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FMBot.Domain;
using FMBot.Persistence.Domain.Models;
using MetaBrainz.MusicBrainz;
using Serilog;

namespace FMBot.Bot.Services
{
    public class MusicBrainzService
    {
        private readonly HttpClient _httpClient;

        public MusicBrainzService(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<ArtistUpdated> AddMusicBrainzDataToArtistAsync(Artist artist)
        {
            try
            {
                var updated = false;

                if (artist.MusicBrainzDate.HasValue && artist.MusicBrainzDate > DateTime.UtcNow.AddDays(-120))
                {
                    return new ArtistUpdated(artist);
                }

                var api = new Query(this._httpClient);

                if (artist.Mbid.HasValue)
                {
                    var musicBrainzArtist = await api.LookupArtistAsync(artist.Mbid.Value);
                    Statistics.MusicBrainzApiCalls.Inc();

                    if (musicBrainzArtist.Name != null)
                    {
                        var startDate = musicBrainzArtist.LifeSpan?.Begin?.NearestDate;
                        var endDate = musicBrainzArtist.LifeSpan?.End?.NearestDate;

                        artist.MusicBrainzDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                        artist.Location = musicBrainzArtist.Area?.Name;
                        artist.CountryCode = musicBrainzArtist.Country;
                        artist.Type = musicBrainzArtist.Type;
                        artist.Disambiguation = musicBrainzArtist.Disambiguation;
                        artist.Gender = musicBrainzArtist.Gender;
                        artist.StartDate = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : null;
                        artist.EndDate = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : null;

                        updated = true;
                    }
                }
                else
                {
                    var musicBrainzResults = await api.FindArtistsAsync(artist.Name, simple: true);
                    Statistics.MusicBrainzApiCalls.Inc();

                    var musicBrainzArtist =
                        musicBrainzResults.Results
                            .OrderByDescending(o => o.Score)
                            .Select(s => s.Item).FirstOrDefault(f => f.Name?.ToLower() == artist.Name.ToLower());

                    if (musicBrainzArtist != null)
                    {

                        var startDate = musicBrainzArtist.LifeSpan?.Begin?.NearestDate;
                        var endDate = musicBrainzArtist.LifeSpan?.End?.NearestDate;

                        artist.MusicBrainzDate = DateTime.SpecifyKind(DateTime.UtcNow,DateTimeKind.Utc);
                        artist.Location = musicBrainzArtist.Area?.Name;
                        artist.CountryCode = musicBrainzArtist.Country;
                        artist.Type = musicBrainzArtist.Type;
                        artist.Disambiguation = musicBrainzArtist.Disambiguation;
                        artist.Gender = musicBrainzArtist.Gender;
                        artist.StartDate = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : null;
                        artist.EndDate = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : null;

                        updated = true;
                    }
                }

                return new ArtistUpdated(artist, updated);
            }
            catch (Exception e)
            {
                Log.Error(e, "error in musicbrainzservice");
                return new ArtistUpdated(artist);
            }
        }

        public record ArtistUpdated(Artist Artist, bool Updated = false);
    }
}
