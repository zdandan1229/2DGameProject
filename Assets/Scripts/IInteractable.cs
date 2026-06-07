using UnityEngine;

public interface IInteractable
{
    Transform InteractionTransform { get; }
    float InteractionDistance { get; }
    void Interact();
}

public interface IInteractionPositionProvider
{
    Vector3 InteractionPosition { get; }
}

public interface IInspectObjectCompleteHandler
{
    bool CompleteInspectObject(string inspectObjectDataId);
}

public interface IInspectObjectCompleteOptionProvider
{
    bool ShouldOpenCompleteDialogue();
}
