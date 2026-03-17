using Zenject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container
            .Bind<LevelBuilder>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<AdManager>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<PlatformManager>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<TileEditor>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<FloatingCanvas>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<UIController>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<ShopLayout>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<Menu>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<PlayerInput>()
            .FromComponentInHierarchy()
            .AsSingle();
        Container
            .Bind<PlayerController>()
            .FromComponentInHierarchy()
            .AsSingle();
    }
}
