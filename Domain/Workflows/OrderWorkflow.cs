using Stateless;

public class OrderWorkflow
{
    private readonly StateMachine<OrderStatus, string> _machine;

    public OrderWorkflow(OrderStatus current)
    {
        _machine = new StateMachine<OrderStatus, string>(() => current, s => current = s);

        _machine.Configure(OrderStatus.Pending)
            .Permit("confirm", OrderStatus.Confirmed)
            .Permit("cancel", OrderStatus.Cancelled);

        _machine.Configure(OrderStatus.Confirmed)
            .Permit("pickup", OrderStatus.Picking);

        _machine.Configure(OrderStatus.Picking)
            .Permit("picked", OrderStatus.Picked);

        _machine.Configure(OrderStatus.Picked)
            .Permit("ship", OrderStatus.InTransit);

        _machine.Configure(OrderStatus.InTransit)
            .Permit("deliver", OrderStatus.Delivering);

        _machine.Configure(OrderStatus.Delivering)
            .Permit("success", OrderStatus.Delivered)
            .Permit("fail", OrderStatus.Failed);

        _machine.Configure(OrderStatus.Failed)
            .Permit("return", OrderStatus.Returning);

        _machine.Configure(OrderStatus.Returning)
            .Permit("done", OrderStatus.Returned);
    }

    public bool Can(string trigger) => _machine.CanFire(trigger);

    public OrderStatus Fire(string trigger)
    {
        _machine.Fire(trigger);
        return _machine.State;
    }
}