#if false
using UnityEngine;
using UnityEngine.UI;

public class Base_MyProfilePopup : Base_UIBase
{
    [SerializeField] private Text Text_Title;
    [SerializeField] private Text Text_Name;
    [SerializeField] private Text Text_Description;
    [SerializeField] private Base_UIButton Btn_Close;
    [SerializeField] private Base_UIButton Btn_BackClose;


    private void OnEnable()
    {
        Btn_Close.BindOnClickButtonEvent(OnClick_Close);
        Btn_BackClose.BindOnClickButtonEvent(OnClick_Close);
    }

    public void OnClick_Close()
    {
        // + 자기자신을 비활성화하는 것이 아니라 꼭! UI 매니저를 통해서 닫기 요청을 해주자
        Base_UIManager.Instance.ClosePopupUI(Base_UIType.MyProfilePopup);
    }

    public void RefreshCharacterUI(string characterDataId)
    {
        var myHero = Base_GameDataManager.Instance.GetCharacterData(characterDataId);

        if (myHero != null)
        {
            Debug.Log($"로드된 캐릭터 이름: {myHero.Name}");
        }

        Text_Name.text = myHero.Name;
        Text_Title.text = myHero.Name;

        string dummyDescription = string.Empty;
        // 스킬 정보가 있다면
        if (myHero.SkillList != string.Empty)
        {
            string[] skillNameList = myHero.SkillList.Split(',');
            foreach (string skillName in skillNameList)
            {
                var skillData = Base_GameDataManager.Instance.GetSkill(skillName);
                if (skillData != null)
                {
                    dummyDescription += $"로드된 캐릭터: {myHero.Name}는 {skillData.Name}을 갖고 있다!";
                }
            }
        }

        Text_Description.text = dummyDescription;

        //if (string.IsNullOrEmpty(myHero.UseWeaponId) == false)
        //{
        //    var weaponData = Base_GameDataManager.Instance.GetWeaponData(myHero.UseWeaponId);
        //    if (weaponData != null)
        //    {
        //        Debug.Log($"로드된 캐릭터: {myHero.Name}는 사용무기로 {weaponData.Name}을 갖고 있다!");
        //    }
        //}
    }
}
#endif
