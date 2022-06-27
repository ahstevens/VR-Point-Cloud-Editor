/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

/// <summary>
/// The Place Photo service, part of the Google Places API Web Service, is a read-only API that allows you to add high quality photographic content to your application. <br/>
/// The Place Photo service gives you access to the millions of photos stored in the Places and Google+ Local database. <br/>
/// When you get place information using a Place Details request, photo references will be returned for relevant photographic content. <br/>
/// The Nearby Search and Text Search requests also return a single photo reference per place, when relevant. <br/>
/// Using the Photo service you can then access the referenced photos and resize the image to the optimal size for your application.
/// </summary>
public class OnlineMapsGooglePlacePhoto : OnlineMapsWebServiceAPI
{
    /// <summary>
    /// Event that occurs when a response is received from Google API.
    /// </summary>
    public Action<Texture2D> OnComplete;

    public new Action<OnlineMapsGooglePlacePhoto> OnFailed;
    public new Action<OnlineMapsGooglePlacePhoto> OnFinish;
    public new Action<OnlineMapsGooglePlacePhoto> OnSuccess;

    private OnlineMapsGooglePlacePhoto(string key, string photo_reference, int? maxWidth, int? maxHeight)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.GoogleMaps();

        StringBuilder builder = new StringBuilder("https://maps.googleapis.com/maps/api/place/photo?key=").Append(key);
        builder.Append("&photo_reference=").Append(photo_reference);
        if (maxWidth.HasValue) builder.Append("&maxwidth=").Append(maxWidth);
        if (maxHeight.HasValue) builder.Append("&maxheight=").Append(maxHeight);

        if (!maxWidth.HasValue && !maxHeight.HasValue) builder.Append("&maxwidth=").Append(800);

        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    private void OnRequestComplete(OnlineMapsWWW www)
    {
        if (www == null || !www.isDone) return;

        _status = www.hasError ? OnlineMapsQueryStatus.error : OnlineMapsQueryStatus.success;

        Texture2D texture = null;

        if (status == OnlineMapsQueryStatus.success)
        {
            texture = new Texture2D(1, 1);
            www.LoadImageIntoTexture(texture);
        }

        if (OnComplete != null) OnComplete(texture);
        if (status == OnlineMapsQueryStatus.success)
        {
            if (OnSuccess != null) OnSuccess(this);
        }
        else
        {
            if (OnFailed != null) OnFailed(this);
        }
        if (OnFinish != null) OnFinish(this);

        _status = OnlineMapsQueryStatus.disposed;
        customData = null;
        this.www = null;
    }

    /// <summary>
    /// Download photo from Google Places.
    /// </summary>
    /// <param name="key">Google Maps API Key</param>
    /// <param name="photo_reference">String used to identify the photo when you perform a Photo request.</param>
    /// <param name="maxWidth">
    /// Specifies the maximum desired width, in pixels, of the image returned by the Place Photos service. <br/>
    /// If the image is smaller than the values specified, the original image will be returned. <br/>
    /// If the image is larger in either dimension, it will be scaled to match the smaller of the two dimensions, restricted to its original aspect ratio. <br/>
    /// maxWidth accept an integer between 1 and 1600.
    /// </param>
    /// <param name="maxHeight">
    /// Specifies the maximum desired height, in pixels, of the image returned by the Place Photos service. <br/>
    /// If the image is smaller than the values specified, the original image will be returned. <br/>
    /// If the image is larger in either dimension, it will be scaled to match the smaller of the two dimensions, restricted to its original aspect ratio. <br/>
    /// maxHeight accept an integer between 1 and 1600.<br/>
    /// </param>
    /// <returns></returns>
    public static OnlineMapsGooglePlacePhoto Download(string key, string photo_reference, int? maxWidth = null, int? maxHeight = null)
    {
        return new OnlineMapsGooglePlacePhoto(key, photo_reference, maxWidth, maxHeight);
    }

    public override void Destroy()
    {
        if (OnDispose != null) OnDispose(this);
        www = null;
        _status = OnlineMapsQueryStatus.disposed;
        customData = null;
        OnComplete = null;
        OnFinish = null;
    }
}