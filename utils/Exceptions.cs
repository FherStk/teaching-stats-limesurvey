public class IncorrectSettingsException : System.Exception{
    public IncorrectSettingsException() : this("Unable to load the settings file, please check the 'config/settings.yml' file."){}
    public IncorrectSettingsException(string message, Exception? innerException = null) : base(message, innerException){}
}

public class SmtpException : System.Exception{
    public SmtpException() : this("Unable to communicate with the SMTP server. Please, try again later."){}
    public SmtpException(string message, Exception? innerException = null) : base(message, innerException){}
}