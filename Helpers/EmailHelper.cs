using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace WorldMusicJam.Helpers;

public static class EmailHelper
{
    public static bool IsValidEmail(string? email)
    {
        if(string.IsNullOrWhiteSpace(email)) 
            return false;
        
        var regex = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
        return regex.IsMatch(email);
    }
    
    public static void SendEmail(IConfiguration configuration, string email, string subject, string body)
    {
        var smtpClient = new SmtpClient
        (
            configuration["MailSettings:Host"] ?? throw new Exception("No api key found"),
            int.Parse(configuration["MailSettings:Port"] ?? throw new Exception("No api key found"))
        );
        smtpClient.EnableSsl = true;
        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtpClient.UseDefaultCredentials = false;
        
        smtpClient.Credentials = new NetworkCredential
        (
            configuration["MailSettings:Api"] ?? throw new Exception("No api key found"),
            configuration["MailSettings:SecretKet"] ?? throw new Exception("No secret found"));
        smtpClient.Send(configuration["MailSettings:Sender"] ?? throw new Exception("No api key found"),
            email,
            subject,
            body
        );
    }
}