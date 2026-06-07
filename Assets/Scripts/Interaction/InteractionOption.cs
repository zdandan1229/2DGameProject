using System;

public enum InteractionActionType
{
    None = 0,
    OpenDialogue,
    OpenStatus,
    OpenInspectObject,
    OpenInspectArea,
    OpenJournal,
    ShowPopupText,
    PickupItem,
    EnterStage,
    ExitMenu
}

[Serializable]
public class InteractionOption
{
    private string _buttonText;
    private InteractionActionType _actionType;

    public string ButtonText
    {
        get { return _buttonText; }
    }

    public InteractionActionType ActionType
    {
        get { return _actionType; }
    }

    public InteractionOption(string buttonText, InteractionActionType actionType)
    {
        _buttonText = buttonText;
        _actionType = actionType;
    }

    public bool IsValid()
    {
        return IsExecutable();
    }

    public bool IsExecutable()
    {
        if (_actionType == InteractionActionType.None)
        {
            return false;
        }

        return true;
    }

    public bool CanShowInMenu()
    {
        return IsExecutable() && string.IsNullOrEmpty(_buttonText) == false;
    }

    public bool ShouldExecuteDirectly()
    {
        return IsExecutable() && string.IsNullOrEmpty(_buttonText);
    }
}
