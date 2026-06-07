using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SortOrderSetting : MonoBehaviour
{
    [SerializeField] private int _sortingOrder = 120;
    [SerializeField] private int _defaultSortingOrder = 150;

    private Collider2D _triggerCollider;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();

        if (_triggerCollider == null)
        {
            Debug.LogError("SortOrderSetting에 Collider2D가 없어 플레이어 정렬 순서를 바꿀 수 없습니다.");
            return;
        }

        if (_triggerCollider.isTrigger == false)
        {
            Debug.LogWarning("SortOrderSetting의 Collider2D가 Trigger가 아니어서 플레이어 진입/이탈을 감지할 수 없습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SetPlayerSortingOrder(collision, _sortingOrder);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        SetPlayerSortingOrder(collision, _sortingOrder);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        SetPlayerSortingOrder(collision, _defaultSortingOrder);
    }

    private void SetPlayerSortingOrder(Collider2D collision, int sortingOrder)
    {
        if (collision == null)
        {
            Debug.LogWarning("SortOrderSetting에 들어온 Collider2D가 비어 있어 정렬 순서를 바꿀 수 없습니다.");
            return;
        }

        Player player = collision.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("Player에 SpriteRenderer가 없어 정렬 순서를 바꿀 수 없습니다.");
            return;
        }

        spriteRenderer.sortingOrder = sortingOrder;
    }
}
