namespace TowerDefense.Save;

public interface ISaveProvider
{
    PlayerProgress Load();
    void Save(PlayerProgress progress);
}
