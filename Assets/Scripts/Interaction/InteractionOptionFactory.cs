using System;
using System.Collections.Generic;
using UnityEngine;

public static class InteractionOptionFactory
{
    public static List<InteractionOption> CreateOptionList(string interactionObjectDataId)
    {
        List<InteractionOption> optionList = new List<InteractionOption>();

        if (string.IsNullOrEmpty(interactionObjectDataId))
        {
            return optionList;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing. Interaction options cannot be loaded.");
            return optionList;
        }

        InteractionObjectData objectData = GameDataManager.Instance.GetInteractionObjectData(interactionObjectDataId);
        if (objectData == null)
        {
            Debug.LogWarning($"InteractionObject data was not found: {interactionObjectDataId}");
            return optionList;
        }

        if (objectData.InteractionOptionIdList == null || objectData.InteractionOptionIdList.Count <= 0)
        {
            Debug.LogWarning($"InteractionObject has no option ids: {interactionObjectDataId}");
            return optionList;
        }

        for (int i = 0; i < objectData.InteractionOptionIdList.Count; i++)
        {
            string optionDataId = objectData.InteractionOptionIdList[i];
            if (string.IsNullOrEmpty(optionDataId))
            {
                Debug.LogWarning($"Interaction option id is empty. object: {interactionObjectDataId}, index: {i}");
                continue;
            }

            InteractionOptionData optionData = GameDataManager.Instance.GetInteractionOptionData(optionDataId);
            if (optionData == null)
            {
                Debug.LogWarning($"InteractionOption data was not found: {optionDataId}");
                continue;
            }

            if (Enum.TryParse(optionData.ActionType, out InteractionActionType actionType) == false)
            {
                Debug.LogWarning($"InteractionOption has an invalid ActionType. id: {optionDataId}, ActionType: {optionData.ActionType}");
                continue;
            }

            InteractionOption interactionOption = new InteractionOption(optionData.ButtonText, actionType, optionData.TargetDataId);
            if (interactionOption.IsValid() == false)
            {
                Debug.LogWarning($"InteractionOption is invalid and will be skipped: {optionDataId}");
                continue;
            }

            optionList.Add(interactionOption);
        }

        return optionList;
    }
}
