using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private static int _consumedPrimaryClickFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InputManager Instance가 이미 존재합니다. 중복 생성된 InputManager를 비활성화합니다.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
    }

    public static Vector2 GetMoveDirection()
    {
        Vector2 moveDirection = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            moveDirection += Vector2.up;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            moveDirection += Vector2.down;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveDirection += Vector2.left;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveDirection += Vector2.right;
        }

        moveDirection.Normalize();
        return moveDirection;
    }

    public static bool GetPrimaryClickDown()
    {
        if (_consumedPrimaryClickFrame == Time.frameCount)
        {
            return false;
        }

        return Input.GetMouseButtonDown(0);
    }

    public static void ConsumePrimaryClickForFrame()
    {
        _consumedPrimaryClickFrame = Time.frameCount;
    }

    public static Vector3 GetPointerScreenPosition(float zPosition)
    {
        Vector3 pointerScreenPosition = Input.mousePosition;
        pointerScreenPosition.z = zPosition;
        return pointerScreenPosition;
    }

    public static bool GetInventoryToggleDown()
    {
        return Input.GetKeyDown(KeyCode.I);
    }

    public static bool GetMiniMapToggleDown()
    {
        return Input.GetKeyDown(KeyCode.M);
    }

    public static bool GetJournalToggleDown()
    {
        return Input.GetKeyDown(KeyCode.J);
    }

    public static bool GetPlayerMenuNextTabDown()
    {
        return Input.GetKeyDown(KeyCode.Tab);
    }

    public static bool GetCloseUIDown()
    {
        return Input.GetKeyDown(KeyCode.X);
    }

    public static bool GetInteractionMenuTestDown()
    {
        return Input.GetKeyDown(KeyCode.U);
    }

    public static bool GetDialogueNextDown()
    {
        return
            Input.GetKeyDown(KeyCode.Z);
    }

    public static bool GetTextWindowToggleDown()
    {
        return Input.GetKeyDown(KeyCode.LeftShift);
    }
}
