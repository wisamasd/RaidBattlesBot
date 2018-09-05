﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoogleMapsApi;
using GoogleMapsApi.Entities.Common;
using GoogleMapsApi.Entities.DistanceMatrix.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Request;
using GoogleMapsApi.Entities.PlacesNearBy.Response;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaidBattlesBot.Configuration;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Handlers
{
  public class GymHelper
  {
    private readonly RaidBattlesContext myDbContext;
    private readonly Gyms myGyms;
    private readonly TelemetryClient myTelemetryClient;
    private readonly GeoCoderConfiguration myGeoCoderOptions;

    public GymHelper(RaidBattlesContext dbContext, Gyms gyms, TelemetryClient telemetryClient, IOptions<GeoCoderConfiguration> geoCoderOptions)
    {
      myDbContext = dbContext;
      myGyms = gyms;
      myTelemetryClient = telemetryClient;
      myGeoCoderOptions = geoCoderOptions.Value ?? throw new ArgumentNullException(nameof(geoCoderOptions));
    }

    public async Task<((decimal? lat, decimal? lon) location, string gym, string distance)> ProcessGym(Raid raid, StringBuilder description, int? precision = null, MidpointRounding? rounding = null, CancellationToken cancellationToken = default)
    {
      var  location = (lat: raid.Lat, lon: raid.Lon);
      string distance = default;
      var gym = raid.Gym ?? raid.PossibleGym;
      if (myGyms.TryGet((decimal)raid.Lat, (decimal)raid.Lon, out var foundGym, precision, rounding) && (foundGym.name is var foundGymName) && (foundGymName != gym))
      {
        if ((gym == null) || foundGymName.StartsWith(gym))
        {
          gym = raid.PossibleGym = foundGymName;
        }
        location = foundGym.location;
      }
      if (gym == null)
      {
        raid.PossibleGym = await myDbContext.Set<Raid>()
          .FindKnownGym((decimal) raid.Lat, (decimal) raid.Lon, precision, rounding)
          .Select(_ => _.Gym ?? _.PossibleGym)
          .FirstOrDefaultAsync(cancellationToken);
      }

      //if (!string.IsNullOrEmpty(gym))
      //{
      //  description.Append(gym);
      //}

      //var geoCode = await myGeoCoder.Decode(lon, lat, cancellationToken: cancellationToken,
      //    parameters: new Dictionary<string, string>
      //    {
      //        {"results", "1"},
      //        {"kind", "metro"}
      //    });
      //var metro = geoCode?.featureMember?.FirstOrDefault()?.GeoObject;

      try
      {
        var geoRequest = InitGeoRequest(new PlacesNearByRequest
        {
          Location = new Location((double)location.lat, (double)location.lon),
          Type = "subway_station",
          RankBy = RankBy.Distance,
        });

        var geoResponse = await GoogleMaps.PlacesNearBy.QueryAsync(geoRequest, myTelemetryClient, cancellationToken);

        Result foundAddress = null;
        Action<StringBuilder> postProcessor = null;
        foreach (var address in geoResponse.Results)
        {
          if (address.Types.Contains("subway_station"))
          {
            var distanceMatrixRequest = InitGeoRequest(new DistanceMatrixRequest
            {
              Origins = new[] { $"place_id:{address.PlaceId}" },
              Destinations = new[] { geoRequest.Location.LocationString },
              Mode = DistanceMatrixTravelModes.walking,
            });
            var distanceMatrixResponse = await GoogleMaps.DistanceMatrix.QueryAsync(distanceMatrixRequest, cancellationToken);

            var distanceElement = distanceMatrixResponse.Rows.FirstOrDefault()?.Elements.FirstOrDefault();

            if (distanceElement?.Distance.Value <= myGeoCoderOptions.MaxDistanceToMetro)
            {
              foundAddress = address;
              postProcessor = descr => descr.Append(distance = $" ∙ {distanceElement.Distance.Text} ∙ {distanceElement.Duration.Text}");
              break;
            }
          }
        }

        if (foundAddress == null)
        {
          geoRequest.Type = "locality";
          foundAddress = (await GoogleMaps.PlacesNearBy.QueryAsync(geoRequest, myTelemetryClient, cancellationToken)).Results.FirstOrDefault();
        }

        if (foundAddress != null)
        {
          raid.NearByPlaceId = foundAddress.PlaceId;
          raid.NearByAddress = foundAddress.Name;
          description
            .Append(description.Length > 0 ? " ∙ " : "")
            .Append(foundAddress.Name);
          
          postProcessor?.Invoke(description);
        }
      }
      catch (Exception ex)
      {
        myTelemetryClient.TrackExceptionEx(ex);
      }

      return (location, gym, distance);
    }

    private TRequest InitGeoRequest<TRequest>(TRequest request)
      where TRequest : MapsBaseRequest
    {
      request.ApiKey = myGeoCoderOptions.GoogleKey;
      if (request is ILocalizableRequest loc)
      {
        loc.Language = CultureInfo.CurrentCulture.Name;
      }
      return request;
    }
  }
}