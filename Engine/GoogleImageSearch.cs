using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;

namespace Engine
{
    public class GoogleImageSearch
    {
        String key; //api key
        String customEngine; //custom search engine id

        public GoogleImageSearch(String apiKey, String cx)
        {
            key = apiKey;
            customEngine = cx;           
        }

        public List<String> Search(String query, int pages=1)
        {
            BaseClientService.Initializer init = new Google.Apis.Services.BaseClientService.Initializer();
            init.ApiKey = key;
            CustomsearchService svc = new CustomsearchService(init);
        
            List<String> imageUrls = new List<String>();

            for (int p = 1; p <= pages; p++)
            {
                CseResource.ListRequest listRequest = svc.Cse.List(query);
                listRequest.Cx = customEngine;
                listRequest.SearchType = CseResource.SearchType.Image;
                listRequest.Start = p*10;
                listRequest.ImgColorType = CseResource.ImgColorType.Color;
                Search search = listRequest.Fetch();

                //Let's just get the thumbnail images
                if (search.Items != null)
                {
                    foreach (Result result in search.Items)
                    {
                        imageUrls.Add(result.Image.ThumbnailLink);
                    }
                }
            }
            return imageUrls;

        }



    }
}
