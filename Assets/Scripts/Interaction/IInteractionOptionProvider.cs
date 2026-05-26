using System.Collections.Generic;
using UnityEngine;

public interface IInteractionOptionProvider
{
    Transform InteractionMenuTransform { get; }
    List<InteractionOption> GetInteractionOptions();
}
