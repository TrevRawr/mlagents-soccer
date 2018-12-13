
public enum OBSERVABLE_TYPE
{
    ZPOSITIVE_NET,
    ZNEGATIVE_NET,
    SOCCER_PLAYER,
}

public interface ObservableSubject {
    OBSERVABLE_TYPE GetObserverType();
    void RegisterObserver(Observer observer);
    void notify();
}
