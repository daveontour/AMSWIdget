using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HttpClientSample
{

    class Program
    {
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            Task.Run(() => MainAsync());
            Console.ReadLine();
        }

        static async Task MainAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost");
//                AuthenticationHeaderValue authValue = new AuthenticationHeaderValue("Basic", "4c1d76c2-33ee-486f-aba3-43ffff0bb2b7");
  //              client.DefaultRequestHeaders.Authorization = authValue;
                client.DefaultRequestHeaders.Add("Autorixation", "4c1d76c2-33ee-486f-aba3-43ffff0bb2b7");

                var result = await client.GetAsync("/api/v1/DOH/CheckIns");
                string resultContent = await result.Content.ReadAsStringAsync();
                Console.WriteLine(resultContent);
            }
        }
    }
}
