namespace WebAPI.Application;

public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(Exception inner)
        : base("Email already exists.", inner)
    {
    }
}
