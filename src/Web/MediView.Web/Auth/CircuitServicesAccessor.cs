namespace MediView.Web.Auth;

public sealed class CircuitServicesAccessor
{
    private static readonly AsyncLocal<IServiceProvider?> CircuitServices = new();

    public IServiceProvider? Services
    {
        get => CircuitServices.Value;
        set => CircuitServices.Value = value;
    }
}
