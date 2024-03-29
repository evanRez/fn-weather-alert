using System.Net;
using System.Text.Json;
using data_weatheralert;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace be_weatheralert
{
    public class ScheduledAlert
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;

        public ScheduledAlert(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ScheduledAlert>();
            _client = new HttpClient();
        }

        [Function("ScheduledAlertTrigger")]
        public async Task AlertAtSix([TimerTrigger("0 */5 * * * *")] Microsoft.Azure.Functions.Worker.TimerInfo myTimer)
        {
            // "0 0 6 * * *" = 6am everyday
            //0 */5 * * * * every 5 min
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            // Get the connection string from app settings and use it to create a connection.
            var str = Environment.GetEnvironmentVariable("SqlConnectionString");
            using (SqlConnection conn = new SqlConnection(str))
            {
                SqlCommand command = new(
                "SELECT Name, Email, Latitude, Longitude FROM dbo.Users;",
                conn);
                conn.Open();

                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        var email = reader.GetString(1);
                        var lat = reader.GetString(2);
                        var lon = reader.GetString(3);
                        if (await IsItGoingToRainToday(lat, lon))
                        {
                            sendEmail(name, email);
                            _logger.LogInformation("Heck yes, get your umbrella");
                        }
                        
                    }
                }
                else
                {
                    _logger.LogInformation("No rows found.");
                }
                reader.Close();
            }
        }


        public void sendEmail(string recipientName, string recipientEmail)
        {
            _logger.LogInformation("Email method was called from Scheduled Alerts");
            try 
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Weather Boy", "steveadler72@gmail.com"));  
                message.To.Add(new MailboxAddress(recipientName, recipientEmail));  
                message.Subject = "Don't forget your umbrella!";  

                message.Body = new TextPart("plain")  
                {  
                    Text = """  
                    Hey Dude,  

                    It's going to rain today, make sure to bring your umbrella out before you leave the house. 

                    -- Your friends at weather alert.  
                    """  
                };  
            
                using var client = new SmtpClient();  
                client.Connect("smtp-relay.brevo.com", 587, false);  
                // Note: only needed if the SMTP server requires authentication  
                // Remove the password and put into azure keyvault or something
                var key = Environment.GetEnvironmentVariable("MailApiKey");
                client.Authenticate("steveadler72@gmail.com", key);  
                client.Send(message);  
                client.Disconnect(true);
                _logger.LogInformation("Successfully send our first email");
            }
            catch (Exception err)
            {
                _logger.LogError(err.Message);
            }
        }


        public async Task<bool> IsItGoingToRainToday(string latitude, string longitude)
        {
            var key = Environment.GetEnvironmentVariable("WeatherApiKey");

            var urlString = $"http://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&cnt=6&appid={key}";
            
            _logger.LogInformation("url string: " + urlString);
            var responseBody = await _client.GetStringAsync(urlString);

            _logger.LogInformation("Weather Data: " + responseBody);

            var response = JsonSerializer.Deserialize<Root>(responseBody);

            var rainBool = response?.list.Any(x => x.weather.Any(y => y.main.ToString().Contains("Rain")));

            if (rainBool.HasValue && rainBool.Value == true)
            {
                _logger.LogInformation("Let it rain");
                return true;
            }
            _logger.LogInformation("Its dry out there");
            return false;

        }

        [Function("CommitToSix")]
        public OutputType CommitToSix([HttpTrigger(
            AuthorizationLevel.Function, 
            "post",
            Route = "CommitToSix")] 
            HttpRequestData req,
            FunctionContext functionContext)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var message = "Welcome to Azure Functions!";

            try
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString(message);

                string requestBody = new StreamReader(req.Body).ReadToEnd();
                _logger.LogInformation("Request Body" + requestBody);
                var user = JsonSerializer.Deserialize<UserDTO>(requestBody);

                var newlyCreatedUser = new UserDTO()
                    {
                        UserId = Guid.NewGuid(),
                        Email = user?.Email ?? "example@test.com",
                        TimeId = 6,
                        Active = true,
                        Name = user?.Name ?? "Tester",
                        Latitude = user.Latitude,
                        Longitude = user.Longitude
                    };

                var serializedUser = JsonSerializer.Serialize(newlyCreatedUser);
                _logger.LogInformation("This newley created user obj: " + serializedUser);

                return new OutputType()
                {
                    UserDTO = newlyCreatedUser,
                    HttpResponse = response
                };

            } catch (Exception err) 
            {
                _logger.LogError(err.Message);
            }

            return new OutputType() 
            {
                UserDTO = new UserDTO(),
                HttpResponse = req.CreateResponse(HttpStatusCode.BadRequest)
            };

            
        }

        
    }
}
