using UnityEngine;

public interface IInteractable
{
    Transform InteractionTransform { get; }
    float InteractionDistance { get; }
    void Interact();
}

public interface IInspectObjectCompleteHandler
{
    void CompleteInspectObject(string inspectObjectDataId);
}
