using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yak;

namespace CompatTests.Util
{
  public class Food4RhinoSource : BaseSource
  {
    const string BaseUrl = "https://www.food4rhino.com";
    //const string RhinoPackagesQuery = "/en/browse?sort_by=ss_node_title&items_per_page=100&f%5B0%5D=im_field_unified_type%3A773&f%5B1%5D=im_field_platform_app%3A722";
    const string RhinoPackagesQuery = "/en/browse?searchText=&sort_by=ss_node_title&items_per_page=10&f%5B0%5D=im_field_unified_type%3A773";


    /* f4r requires users to log in before downloading anything
     * we need to spoof this
     * go to https://www.food4rhino.com log in, then inspect the cookie
     * the string you're after should be similar to the one below (not a real cookie!)
     * 'SESSaa7e25adfc00ac8a6028373e69143523=acim0ajfgkbh4mki8t7j2hobs1'
     * 
     * Change this string with the new values.
     */
    const string AuthCookieName = "SSESS9487ea0fb540f9ff81f1888d4c131955";
    const string AuthCookieValue = "L5lkj9SrHQO0rsXkWRnLqMGhRDlLGPOsr8LSzjdPzyM";

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
      static string[] AllowedZipExtensions = { ".rhi", ".zip" };

      public async Task<string?> Download()
      {
        var outputPath = Path.Combine(_source.OutputPath, Id);

        // if we've already downloaded this plugin, return the path
        if (Directory.Exists(outputPath) && Directory.GetFiles(outputPath).Length > 0)
          return outputPath;

        // always create directory for the plugin, even if we don't find anything to download, so we don't try again next time
        if (!Directory.Exists(outputPath))
          Directory.CreateDirectory(outputPath);
          
        Console.WriteLine($"Downloading {Name} ({Id}) from {Url}");

        var pluginUrl = BaseUrl + Url;
        using var client = _source.CreateClient();
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

        // The downloads list is no longer rendered into the page directly, it is
        // loaded afterwards via an ajax request to the lazy_pane module. Fetch it
        // and parse the returned html fragment for the download links.
        var downloadsDocument = await GetDownloadsDocument(client, pluginUrl, pluginPageHtml, document, parser);

        bool hasYakPackage = false;
        var items = new List<(string url, int rhinoVersion)>();
        foreach (var downloadItem in downloadsDocument.QuerySelectorAll(".app_downloads_list_item"))
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
          if (string.IsNullOrEmpty(platforms))
            continue;
            
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

        foreach (var item in latestRhinoVersionItems)
        {
          var fileName = item.url.Substring(item.url.LastIndexOf('/') + 1);


          var destFile = Path.Combine(outputPath, fileName);

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
            
            // unzip if it's a zip or rhi (which is a zip) so we can find the rhi/gha inside
            // also unzip any .zip files inside the zip (some plugins ship with a zip of .rhi's)            
            if (!AllowedZipExtensions.Any(r => fileName.EndsWith(r, StringComparison.OrdinalIgnoreCase)))
              continue;
            

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

      /// <summary>
      /// The download list on a plugin page is now lazy-loaded via an ajax request to the
      /// lazy_pane module (https://www.food4rhino.com/en/lazy-pane/ajax). This fetches that
      /// fragment and returns it as a parsed document so the download links can be scraped.
      /// Falls back to the original page document if the lazy pane can't be found/loaded.
      /// </summary>
      async Task<IHtmlDocument> GetDownloadsDocument(HttpClient client, string pluginUrl, string pluginPageHtml, IHtmlDocument pageDocument, HtmlParser parser)
      {
        // find the lazy pane placeholder for the downloads (app_files) view
        var paneId = pageDocument.QuerySelectorAll(".lazy-pane-placeholder")
          .Select(r => r.GetAttribute("data-lazy-pane-id"))
          .FirstOrDefault(r => r?.IndexOf("views:app_files", StringComparison.OrdinalIgnoreCase) >= 0);

        if (string.IsNullOrEmpty(paneId))
          return pageDocument; // no lazy pane, downloads may be inline (older page)

        // these values come from the Drupal.settings json embedded in the page, and are
        // required by the ajax endpoint (it returns 403 without a valid theme_token)
        var theme = Regex.Match(pluginPageHtml, "\"theme\":\"([^\"]+)\"").Groups[1].Value;
        var themeToken = Regex.Match(pluginPageHtml, "\"theme_token\":\"([^\"]+)\"").Groups[1].Value;
        var currentPath = Regex.Match(pluginPageHtml, "\"current_path\":\"([^\"]+)\"").Groups[1].Value.Replace("\\/", "/");

        var formData = new List<KeyValuePair<string, string>>
        {
          new KeyValuePair<string, string>("lazy_pane_ids[]", paneId),
          new KeyValuePair<string, string>("lazy_pane_current_path", currentPath),
          new KeyValuePair<string, string>("ajax_page_state[theme]", theme),
          new KeyValuePair<string, string>("ajax_page_state[theme_token]", themeToken),
        };

        var ajaxRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/en/lazy-pane/ajax")
        {
          Content = new FormUrlEncodedContent(formData)
        };
        // the endpoint requires these or it returns 403 Forbidden
        ajaxRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
        ajaxRequest.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
        ajaxRequest.Headers.Referrer = new Uri(pluginUrl);

        var ajaxResult = await client.SendAsync(ajaxRequest);
        if (!ajaxResult.IsSuccessStatusCode)
        {
          Debug.WriteLine($"ERROR: Couldn't load lazy pane downloads for {pluginUrl}. {ajaxResult}");
          return pageDocument;
        }

        // the response is a json array of drupal ajax commands; the "insert" command's
        // "data" property contains the html fragment for the downloads list
        var json = await ajaxResult.Content.ReadAsStringAsync();
        string? data = null;
        try
        {
          var commands = JArray.Parse(json);
          data = commands
            .FirstOrDefault(c => (string?)c["command"] == "insert")?["data"]?.ToString();
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"ERROR: Couldn't parse lazy pane response for {pluginUrl}. {ex}");
        }

        if (string.IsNullOrEmpty(data))
          return pageDocument;

        return await parser.ParseDocumentAsync(data);
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
    
    HttpClient CreateClient()
    {
      var handler = new HttpClientHandler();
      handler.CookieContainer.Add(new Cookie(AuthCookieName, AuthCookieValue, "/", ".www.food4rhino.com"));
      handler.CookieContainer.Add(new Cookie("aucp13n", "kdifl4", "/", ".www.food4rhino.com"));
      var client = new HttpClient(handler);
      // add user agent
      client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.5 Safari/605.1.15");
      return client;
    }

    public override async IAsyncEnumerable<IPackageSource> GetPackages()
    {
      using var client = CreateClient();
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
