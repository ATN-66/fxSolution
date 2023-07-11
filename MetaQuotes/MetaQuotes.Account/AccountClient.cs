namespace MetaQuotes.Account;

public class AccountClient
{
    private const int Deadline = int.MaxValue;
    private readonly int _maxSendMessageSize;
    private readonly int _maxReceiveMessageSize;
    private readonly string _grpcChannelAddress;

    public AccountClient()
    {
        _grpcChannelAddress = @"http://localhost:48052";
        _maxSendMessageSize = 1024 * 1024 * 50;
        _maxReceiveMessageSize = 1024 * 1024 * 50;
    }

    public string SetAccountInfo(long accountNumber)
    {
        return accountNumber.ToString();
    }
}