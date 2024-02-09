using System;
using System.Configuration;
using System.Data;
using System.Net;
using System.Text.Json;
using cl_weatheralert;
using Grpc.Net.Client.Balancer;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;
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

        [Function("ScheduledAlertTrigger")]
        public void AlertAtSix([TimerTrigger("0 0 6 * * *")] TimerInfo myTimer)
        {
            // "0 0 6 * * *" = 6am everyday
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
                var myName = "Not Steve";
                var myEmail = "ereznice@gmail.com";
                sendEmail(myName, myEmail);
            }


        }

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
                client.Authenticate("steveadler72@gmail.com", "08VQHTxmw5RMCnkY");  
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

            String requestBody = new StreamReader(req.Body).ReadToEnd();
            var user = new UserDTO();
            user = JsonSerializer.Deserialize<UserDTO>(requestBody);

            return new OutputType()
            {
                UserDTO = new UserDTO()
                {
                    UserId = Guid.NewGuid(),
                    Email = user.Email,
                    TimeId = 6,
                    Active = 1
                },
                HttpResponse = response
            };
        }

        
    }
}
