using System;

public enum InteractionActionType
{
    None = 0,
    OpenDialogue,
    OpenStatus,
    OpenInspectObject,
    PickupItem,
    EnterDoor,
    ExitMenu
}

[Serializable]
public class InteractionOption
{
    private string _buttonText;
    private InteractionActionType _actionType;
    private string _targetDataId;

    public string ButtonText
    {
        get { return _buttonText; }
    }

    public InteractionActionType ActionType
    {
        get { return _actionType; }
    }

    public string TargetDataId
    {
        get { return _targetDataId; }
    }

    public InteractionOption(string buttonText, InteractionActionType actionType, string targetDataId)
    {
        _buttonText = buttonText;
        _actionType = actionType;
        _targetDataId = targetDataId;
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(_buttonText))
        {
            return false;
        }

        if (_actionType == InteractionActionType.None)
        {
            return false;
        }

        return true;
    }
}
