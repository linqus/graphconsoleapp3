using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using Helpers;

namespace graphconsoleapp
{
    public class Program
    {

        private static GraphServiceClient GetAuthenticatedGraphClient(IConfigurationRoot config)
        {
            var authenticationProvider = CreateAuthorizationProvider(config);
            var graphClient = new GraphServiceClient(authenticationProvider);
            return graphClient;
        }

        private static IAuthenticationProvider CreateAuthorizationProvider(IConfigurationRoot config)
        {
            var clientId = config["applicationId"];
            var authority = $"https://login.microsoftonline.com/{config["tenantId"]}/v2.0";

            List<string> scopes = new List<string>();
            scopes.Add("https://graph.microsoft.com/.default");

            var cca = PublicClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build();
            return MsalAuthenticationProvider.GetInstance(cca, scopes.ToArray());
        }

        private static IConfigurationRoot? LoadAppSettings()
        {
            try
            {
                var config = new ConfigurationBuilder()
                                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                    .AddJsonFile("appconfig.json", false, true)
                                    .Build();

                if (string.IsNullOrEmpty(config["applicationId"]) ||
                    string.IsNullOrEmpty(config["tenantId"]))
                {
                    return null;
                }

                return config;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
        }


        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var config = LoadAppSettings();
            if (config == null)
            {
                Console.WriteLine("Invalid appconfig.json file.");
                return;
            }
            var client = GetAuthenticatedGraphClient(config);
            // request 1 - current user's photo

            var requestUserPhoto = client.Me.Photo.Request();
            var resultsUserPhoto = requestUserPhoto.GetAsync().Result;
            // display photo metadata
            Console.WriteLine("                Id: " + resultsUserPhoto.Id);
            Console.WriteLine("media content type: " + resultsUserPhoto.AdditionalData["@odata.mediaContentType"]);
            Console.WriteLine("        media etag: " + resultsUserPhoto.AdditionalData["@odata.mediaEtag"]);

            Console.WriteLine("\nGraph Request:");
            Console.WriteLine(requestUserPhoto.GetHttpRequestMessage().RequestUri);

            // get actual photo
            var requestUserPhotoFile = client.Me.Photo.Content.Request();
            var resultUserPhotoFile = requestUserPhotoFile.GetAsync().Result;

            // create the file
            var profilePhotoPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "profilePhoto_" + resultsUserPhoto.Id + ".jpg");
            var profilePhotoFile = System.IO.File.Create(profilePhotoPath);
            resultUserPhotoFile.Seek(0, System.IO.SeekOrigin.Begin);
            resultUserPhotoFile.CopyTo(profilePhotoFile);
            Console.WriteLine("Saved file to: " + profilePhotoPath);

            Console.WriteLine("\nGraph Request:");
            Console.WriteLine(requestUserPhoto.GetHttpRequestMessage().RequestUri);

        }
    }
}