using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class InteractionClickArea : MonoBehaviour
{
    public class InteractionClickResult
    {
        public IInteractionOptionProvider OptionProvider { get; private set; }
        public Vector3 InteractionPosition { get; private set; }

        public InteractionClickResult(IInteractionOptionProvider optionProvider, Vector3 interactionPosition)
        {
            OptionProvider = optionProvider;
            InteractionPosition = interactionPosition;
        }
    }

    public static IInteractionOptionProvider FindOptionProviderAtWorldPosition(Vector3 worldPosition, bool shouldLogMissingProvider)
    {
        InteractionClickResult clickResult = FindClickResultAtWorldPosition(worldPosition, shouldLogMissingProvider);
        return clickResult != null ? clickResult.OptionProvider : null;
    }

    public static InteractionClickResult FindClickResultAtWorldPosition(Vector3 worldPosition, bool shouldLogMissingProvider)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);
        if (colliders == null || colliders.Length <= 0)
        {
            return null;
        }

        InteractionClickArea selectedClickArea = null;
        Collider2D selectedCollider = null;
        int selectedSortingOrder = int.MinValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            InteractionClickArea clickArea = collider.GetComponent<InteractionClickArea>();
            if (clickArea == null)
            {
                continue;
            }

            int sortingOrder = GetSortingOrder(clickArea);
            if (selectedClickArea == null || sortingOrder > selectedSortingOrder)
            {
                selectedClickArea = clickArea;
                selectedCollider = collider;
                selectedSortingOrder = sortingOrder;
            }
        }

        if (selectedClickArea == null || selectedCollider == null)
        {
            return null;
        }

        IInteractionOptionProvider optionProvider = selectedClickArea.GetComponentInParent<IInteractionOptionProvider>();
        if (optionProvider == null)
        {
            if (shouldLogMissingProvider)
            {
                Debug.LogWarning($"{selectedClickArea.gameObject.name} has InteractionClickArea, but no interaction option provider was found in its parents.");
            }

            return null;
        }

        Vector3 interactionPosition = selectedCollider.ClosestPoint(worldPosition);
        interactionPosition.z = selectedClickArea.transform.position.z;
        return new InteractionClickResult(optionProvider, interactionPosition);
    }

    private static int GetSortingOrder(InteractionClickArea clickArea)
    {
        if (clickArea == null)
        {
            return int.MinValue;
        }

        SpriteRenderer spriteRenderer = clickArea.GetComponentInParent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return int.MinValue;
        }

        return spriteRenderer.sortingOrder;
    }
}
