using System;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emailer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Emailer
{
    public static class SendEmail
    {
        [FunctionName("SendEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST")]
            HttpRequest request,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation("SendEmail requested.");

            if (request.Body == null)
            {
                log.LogWarning("SendEmail without HTTP request body.");
                return new BadRequestObjectResult("Expecting email data in the request body.");
            }

            EmailData emailData = null;
            var serializer = JsonSerializer.Create();
            using (var reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                emailData = (EmailData) serializer.Deserialize(reader, typeof(EmailData));
            }

            if (!ValidateEmailData(emailData))
            {
                log.LogError("Send email failed: missing email data.");
                return new BadRequestObjectResult("Missing email data: sender name, address, subject, or message.");
            }

            await SendGridEmail(emailData, cancellationToken);

            log.LogInformation("Email sent successfully from '{SenderName}'<{SenderEmail}>.", emailData.SenderName,
                emailData.SenderEmail);
            return new OkObjectResult("Email sent successfully.");
        }

        private static Task SendGridEmail(EmailData emailData, CancellationToken cancellationToken)
        {
            var settingApiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var settingRecipient = Environment.GetEnvironmentVariable("SENDGRID_RECIPIENT");

            var email = new SendGridMessage();
            email.From = new EmailAddress(emailData.SenderEmail, emailData.SenderName);
            email.AddTo(settingRecipient);
            email.Subject = emailData.Subject.Normalize(NormalizationForm.FormKD);
            var message = string.Format($"Sender Name:{0}\n\rSender Email: {1}\n\rMessage: {2}", emailData.SenderName, emailData.SenderEmail,
                emailData.Message);
            email.PlainTextContent = message.Normalize(NormalizationForm.FormKD);

            var client = new SendGridClient(settingApiKey);
            return client.SendEmailAsync(email, cancellationToken);
        }

        private static bool ValidateEmailData(EmailData emailData)
        {
            if (emailData == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(emailData.SenderName) ||
                string.IsNullOrWhiteSpace(emailData.SenderEmail) ||
                string.IsNullOrWhiteSpace(emailData.Subject) ||
                string.IsNullOrWhiteSpace(emailData.Message))
            {
                return false;
            }

            try
            {
                var mailAddress = new MailAddress(emailData.SenderEmail, emailData.SenderName);
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }
    }
}
