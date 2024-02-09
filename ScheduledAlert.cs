using System.Net;
using System.Text.Json;
using data_weatheralert;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace be_weatheralert
{
    public class ScheduledAlert
    {
        private readonly ILogger _logger;

        public ScheduledAlert(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ScheduledAlert>();
        }

        // [Function("ScheduledAlertTrigger")]
        // public void AlertAtSix([TimerTrigger("0 0 6 * * *")] Microsoft.Azure.Functions.Worker.TimerInfo myTimer)
        // {
        //     // "0 0 6 * * *" = 6am everyday
        //     _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
        //     if (myTimer.ScheduleStatus is not null)
        //     {
        //         _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        //         var str = Environment.GetEnvironmentVariable("SqlConnectionString");
        //         // List to store the retrieved data
        //         List<string[]> resultList = new List<string[]>();


        //         using (SqlConnection conn = new SqlConnection(str))
        //         {
        //             conn.Open();
        //             var text = "SELECT Email, Name from dbo.Users"
        //             + "WHERE TimerId = 6";

        //             using (SqlCommand cmd = new SqlCommand(text, conn))
        //             {
        //                 using (SqlDataReader reader = cmd.ExecuteReader())
        //                 {
        //                     // Iterate through the result set
        //                     while (reader.Read())
        //                     {
        //                         // Retrieve data from each row and store it in an array
        //                         string[] rowData = new string[reader.FieldCount];
        //                         for (int i = 0; i < reader.FieldCount; i++)
        //                         {
        //                             rowData[i] = reader[i].ToString();
        //                         }

        //                         // Add the row data to the list
        //                         resultList.Add(rowData);
        //                     }
        //                 }

        //             }

                    
        //         }

        //         // Display the retrieved data (optional)
        //         foreach (var row in resultList)
        //         {
        //             Console.WriteLine(string.Join(", ", row));
        //             var myEmail = row.ElementAt(0);
        //             var myName = row.ElementAt(1);
        //             sendEmail(myName, myEmail);
        //         }
                
        //     }


        // }

        //https://learn.microsoft.com/en-us/azure/azure-functions/functions-scenario-database-table-cleanup?source=recommendations

        public void sendEmail(string recipientName, string recipientEmail)
        {
            _logger.LogInformation("Email method was called from Scheduled Alerts");
            try 
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Steve Adler", "steveadler72@gmail.com"));  
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

                var user2 = JsonSerializer.Serialize(user);
                _logger.LogInformation("maybe its a serializer issue: " + user2);

                var newlyCreatedUser = new UserDTO()
                    {
                        UserId = Guid.NewGuid(),
                        Email = user?.Email ?? "example@test.com",
                        TimeId = 6,
                        Active = true,
                        Name = user?.Name ?? "Tester"
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
                Console.WriteLine(err.Message);
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
