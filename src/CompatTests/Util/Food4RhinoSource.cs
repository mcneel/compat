using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yak;

namespace CompatTests.Util
{
  public class Food4RhinoSource : BaseSource
  {
    const string BaseUrl = "https://www.food4rhino.com";
    //const string RhinoPackagesQuery = "/en/browse?sort_by=ss_node_title&items_per_page=100&f%5B0%5D=im_field_unified_type%3A773&f%5B1%5D=im_field_platform_app%3A722";
    const string RhinoPackagesQuery = "/en/browse?sort_by=ss_node_title&items_per_page=100&f%5B0%5D=im_field_unified_type%3A773";


    /* f4r requires users to log in before downloading anything
     * we need to spoof this
     * go to https://www.food4rhino.com log in, then inspect the cookie
     * the string you're after should be similar to the one below (not a real cookie!)
     * 'SESSaa7e25adfc00ac8a6028373e69143523=acim0ajfgkbh4mki8t7j2hobs1'
     * 
     * Change this string with the new values.
     */
    const string AuthCookieName = "SSESS9487ea0fb540f9ff81f1888d4c131955";
    const string AuthCookieValue = "CDdn11cc0SfF1bjrf1x3Di3J2H1V10HQwLyOJ4DFJvU";

    public Food4RhinoSource() : base("f4r")
    {
    }

    public class F4RPackageSource : IPackageSource
    {
      private Food4RhinoSource _source;

      public F4RPackageSource(Food4RhinoSource food4RhinoSource, string name, string url, string id)
      {
        _source = food4RhinoSource;
        Name = name;
        Url = url;
        Id = id;
      }

      public string Name { get; }
      public string Url { get; }
      public string Id { get; }

      static string[] AllowedExtensions = { ".rhi", ".zip", ".exe", ".gha" };

      public async Task<string> Download()
      {
        var outputPath = Path.Combine(_source.OutputPath, Id);

        var pluginUrl = BaseUrl + Url;
        using var handler = new HttpClientHandler();
        handler.CookieContainer.Add(new Cookie(AuthCookieName, AuthCookieValue, "/", ".www.food4rhino.com"));

        using var client = new HttpClient(handler);
        var pageRequest = new HttpRequestMessage(HttpMethod.Get, pluginUrl);
        var pluginPageResult = await client.SendAsync(pageRequest);
        if (!pluginPageResult.IsSuccessStatusCode)
        {
          Console.WriteLine($"Cannot retrieve {pluginUrl}");
          return null;
        }
        var pluginPageHtml = await pluginPageResult.Content.ReadAsStringAsync();
        
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(pluginPageHtml);

        bool hasYakPackage = false;
        var items = new List<(string url, int rhinoVersion)>();
        foreach (var downloadItem in document.QuerySelectorAll(".app_downloads_list_item"))
        {
          string? downloadUrl = null;
          foreach (var fileInfo in downloadItem.QuerySelectorAll(".app_downloads_list_item_file > .app_downloads_list_item_file_inner > a"))
          {
            if (fileInfo.GetAttribute("hreforig")?.StartsWith("rhino:") == true)
            {
              hasYakPackage = true;
            }
            var url = fileInfo.GetAttribute("href");
            if (string.IsNullOrEmpty(url) || url?.IndexOf("/user/login", StringComparison.OrdinalIgnoreCase) >= 0)
              continue;

            downloadUrl = url;
            break;
          }

          if (downloadUrl == null)
            continue;

          if (!AllowedExtensions.Any(r => downloadUrl.EndsWith(r, StringComparison.OrdinalIgnoreCase)))
            continue;

          // get latest version supported
          var platforms = downloadItem.QuerySelector(".app_downloads_list_item_platform_inner")?.Text();
          var rhinoVersion = Regex.Matches(platforms, "Rhino\\s+(\\d+)").OfType<Match>()
            .Select(r => int.Parse(r.Groups[1].Value))
            .OrderByDescending(r => r)
            .FirstOrDefault();

          if (rhinoVersion <= 5)
            continue;

          items.Add((downloadUrl, rhinoVersion));
        }

        var latestRhinoVersionItems = items.GroupBy(r  => r.rhinoVersion).OrderByDescending(r => r.Key).FirstOrDefault();
        if (latestRhinoVersionItems == null)
        {
          if (hasYakPackage)
            Console.WriteLine($"{Name} ({Id}) is a yak package");
          else
            Console.WriteLine($"{Name} ({Id}) has nothing to download");
          return null;
        }

        System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


        foreach (var item in latestRhinoVersionItems)
        {
          var fileName = item.url.Substring(item.url.LastIndexOf('/') + 1);


          var destFile = Path.Combine(outputPath, fileName);

          if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

          if (File.Exists(destFile))
            break;


          Console.WriteLine($"Downloading {item.url}");
          Debug.WriteLine($"Downloading {item.url}");
          // need to feed f4r a cookie to give us the plugin
          var downloadRequest = new HttpRequestMessage(HttpMethod.Get, item.url);
          downloadRequest.Headers.Add("Referer", pluginUrl);

          var downloadResult = await client.SendAsync(downloadRequest);
          if (downloadResult.IsSuccessStatusCode)
          {
            var stream = await downloadResult.Content.ReadAsStreamAsync();
            using (var fileStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
            {
              stream.CopyTo(fileStream);
            }

            var zipDir = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(destFile));
            if (!Directory.Exists(zipDir))
              Directory.CreateDirectory(zipDir);
            try
            {
              ZipFile.ExtractToDirectory(destFile, zipDir);

              foreach (var childExt in AllowedExtensions)
              {
                // if the file was a .zip containing any .rhi's, extract them too
                foreach (var childFile in Directory.GetFiles(zipDir, "*" + childExt, SearchOption.AllDirectories))
                {
                  var childDir = Path.Combine(zipDir, Path.GetFileNameWithoutExtension(childFile));
                  if (!Directory.Exists(childDir))
                    Directory.CreateDirectory(childDir);

                  try
                  {
                    ZipFile.ExtractToDirectory(childFile, childDir);
                  }
                  catch (Exception ex)
                  {
                    // not a zip file?
                    Debug.WriteLine($"ERROR: Couldn't unzip {childFile}. {ex}");
                  }
                }
              }
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"ERROR: Couldn't unzip {destFile}. {ex}");
              // not a zip file?
            }
          }
          else
          {
            // for some reason it keeps getting forbidden.. /:
            Debug.WriteLine($"ERROR: Couldn't download {fileName}. {downloadResult}");
          }
          // so we don't hammer the server
          await Task.Delay(1000);


        }

        return outputPath;
      }

      public override string ToString() => Id;
    }


    private IEnumerable<IPackageSource> ParseEntries(IHtmlDocument document)
    {
      foreach (var element in document.QuerySelectorAll(".f4r_list_link_row"))
      {
        var title = element.QuerySelector(".f4r_list_content_title")?.Text();
        if (title == null)
          continue;
        
        var url = element.GetAttribute("href");
        if (url == null)
          continue;

        var id = url.Substring(url.LastIndexOf('/') + 1);
        yield return new F4RPackageSource(this, title.Trim(), url, id);
      }
    }

    public override async IAsyncEnumerable<IPackageSource> GetPackages()
    {
      using var client = new HttpClient();
      var url = BaseUrl + RhinoPackagesQuery;

      var parser = new HtmlParser();
      var results = Enumerable.Empty<IPackageSource>();
      bool hasMore;
      do
      {
        var html = await client.GetStringAsync(url);
        var document = parser.ParseDocument(html);

        foreach (var entry in ParseEntries(document))
        {
          yield return entry;
        }

        // get next page url
        hasMore = false;
        var nextPageUrl = document.QuerySelector(".item-list-pager .pager-next > a")?.GetAttribute("href");
        if (!string.IsNullOrEmpty(nextPageUrl))
        {
          url = BaseUrl + nextPageUrl;
          hasMore = true;
        }
      }
      while (hasMore);
    }
  }
}
