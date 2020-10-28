namespace Core.Modularity.Abstraction
{
    public interface ICoreModule
    {
        void PreConfigureServices(ServiceCollectionContext context);
        void ConfigureServices(ServiceCollectionContext context);
        void PostConfigureServices(ServiceCollectionContext context);


        void PreConfigure(ApplicationBuilderContext context);
        void Configure(ApplicationBuilderContext app);

        void PostConfigure(ApplicationBuilderContext context);

        void Shutdown(ShutdownApplicationContext context);
    }
}
