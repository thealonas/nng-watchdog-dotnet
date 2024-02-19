using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using nng_watchdog.Providers;
using nng.VkFrameworks;
using VkNet.Utils;

namespace nng_watchdog.Helpers;

public class PhotoHelper
{
    private static readonly Uri PhotoUrl = new("https://nng.alonas.ml/assets/images/style/logo/png/main.png");
    private readonly HttpClient _client;

    private readonly VkProvider _provider;

    public PhotoHelper(VkProvider provider, HttpClient client)
    {
        _provider = provider;
        _client = client;
    }

    private byte[] DownloadPhoto()
    {
        return _client.GetByteArrayAsync(PhotoUrl).GetAwaiter().GetResult();
    }

    private static MultipartFormDataContent FormContent(byte[] file, string fileExtension)
    {
        var requestContent = new MultipartFormDataContent();
        var content = new ByteArrayContent(file);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        requestContent.Add(content, "file", $"file.{fileExtension}");
        return requestContent;
    }

    private string PostAndReturnResponse(string serverUrl, HttpContent content)
    {
        var response = _client.PostAsync(serverUrl, content).GetAwaiter().GetResult();
        return Encoding.Default.GetString(response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult());
    }

    private string UploadFile(string serverUrl, byte[] file, string fileExtension, int x, int y, int w)
    {
        var requestContent = FormContent(file, fileExtension);
        requestContent.Add(new StringContent($"{x},{y},{w}"), "_square_crop");
        return PostAndReturnResponse(serverUrl, requestContent);
    }

    private string GetUploadServer(long group)
    {
        return VkFrameworkExecution.ExecuteWithReturn(() =>
            _provider.VkProcessor.VkFramework.Api.Photo.GetOwnerPhotoUploadServer(-group).UploadUrl);
    }

    public void SetAvatar(long group)
    {
        var file = DownloadPhoto();
        var serverUri = GetUploadServer(group);

        var photoResult = JObject.Parse(UploadFile(serverUri, file, "png", 5000, 5000, 5000));

        var hash = photoResult["hash"] ?? throw new InvalidOperationException();
        var photo = photoResult["photo"] ?? throw new InvalidOperationException();
        var server = photoResult["server"] ?? throw new InvalidOperationException();

        var saveResult = VkFrameworkExecution.ExecuteWithReturn(() =>
            _provider.VkProcessor.VkFramework.Api.Call("photos.saveOwnerPhoto", new VkParameters(
                new Dictionary<string, string>
                {
                    {"hash", hash.ToString()},
                    {"photo", photo.ToString()},
                    {"server", server.ToString()}
                })));

        var postId = long.Parse((saveResult["post_id"] ?? throw new InvalidOperationException()).ToString());

        _provider.VkProcessor.VkFramework.DeletePost(group, postId);
    }
}
