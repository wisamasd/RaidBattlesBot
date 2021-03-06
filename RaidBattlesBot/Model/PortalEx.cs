using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using SimpleBase;

namespace RaidBattlesBot.Model
{
  public static class PortalEx
  {
    [CanBeNull]
    public static Uri GetImage(this Portal portal, IUrlHelper urlHelper, int? thumbnail = null, bool fallbackToDefault = true)
    {
      if (portal?.Image is string image && !string.IsNullOrEmpty(image))
      {
        var imageUrl = new Uri(image);
        return thumbnail is int size ?
          new UriBuilder(image) { Path = $"{imageUrl.AbsolutePath}=s{size}-c" }.Uri : imageUrl;
      }

      return fallbackToDefault ? urlHelper.AssetsContent("static_assets/png/btn_pokestop.png") : null;
    }

    public static string EncodeGuid(this Portal portal)
    {
      var delimeterPosition = portal.Guid.IndexOf('.');
      var binaryGuid = new byte[17];
      if (byte.TryParse(portal.Guid.AsSpan(delimeterPosition + 1), out binaryGuid[16]) &&
          Guid.TryParse(portal.Guid.AsSpan(0, delimeterPosition), out var portalGuid) &&
          portalGuid.TryWriteBytes(binaryGuid))
      {
        return Base58.Flickr.Encode(binaryGuid);
      }

      return null;
    }

    [CanBeNull]
    public static string DecodeGuid(ReadOnlySpan<char> encodedGuid)
    {
      try
      {
        var binaryEncodedGuid = Base58.Flickr.Decode(encodedGuid);
        if (binaryEncodedGuid.Length != 17) return null;
        var guid = new Guid(binaryEncodedGuid.Slice(0, 16));
        return $"{guid:N}.{binaryEncodedGuid[16]:D}";
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}