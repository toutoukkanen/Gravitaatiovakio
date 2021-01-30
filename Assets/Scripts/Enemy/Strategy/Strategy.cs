namespace Enemy
{
    // Strategy forces all inheriting strategies to constructor inject a context
    public abstract class Strategy
    {
        protected readonly IContext context;

        protected Strategy(IContext context) => this.context = context;
    }
}