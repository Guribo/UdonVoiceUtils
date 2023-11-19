using System;
using JetBrains.Annotations;
using TLP.UdonUtils;
using TLP.UdonUtils.Events;
using TLP.UdonVoiceUtils.Runtime.Core;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonVoiceUtils.Runtime.Examples
{

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[DefaultExecutionOrder(ExecutionOrder)]
public class DynamicPrivacy : TlpBaseBehaviour
{
    protected override int ExecutionOrderReadOnly => ExecutionOrder;

    [PublicAPI]
    public new const int ExecutionOrder = TlpExecutionOrder.AudioStart;

    [SerializeField]
    private UdonEvent LocalPlayerAdded;

    [SerializeField]
    private UdonEvent LocalPlayerRemoved;

    [SerializeField]
    private PlayerAudioOverride OverrideWithDynamicPriority;


    [SerializeField]
    private int PlayerAddedPrivacyChannelId;

    [SerializeField]
    private int PlayerExitedPrivacyChannelId;

    public void Start()
    {
        #region TLP_DEBUG

#if TLP_DEBUG
        DebugLog(nameof(Start));
#endif

        #endregion

        if (!Utilities.IsValid(LocalPlayerAdded))
        {
            ErrorAndDisableComponent($"{nameof(LocalPlayerAdded)} is not set");
            return;
        }

        if (!Utilities.IsValid(LocalPlayerRemoved))
        {
            ErrorAndDisableComponent($"{nameof(LocalPlayerRemoved)} is not set");
            return;
        }

        if (!Utilities.IsValid(OverrideWithDynamicPriority))
        {
            ErrorAndDisableComponent($"{nameof(OverrideWithDynamicPriority)} is not set");
        }
    }

    public void OnEnable()
    {
        #region TLP_DEBUG

#if TLP_DEBUG
        DebugLog(nameof(OnEnable));
#endif

        #endregion

        if (!Utilities.IsValid(LocalPlayerAdded) ||
            !LocalPlayerAdded.AddListenerVerified(this, nameof(OnLocalPlayerAdded)))
        {
            ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerAdded)}");
            return;
        }

        if (!Utilities.IsValid(LocalPlayerRemoved) ||
            !LocalPlayerRemoved.AddListenerVerified(this, nameof(OnLocalPlayerRemoved)))
        {
            ErrorAndDisableComponent($"Failed to listen to {nameof(LocalPlayerRemoved)}");
        }
    }

    public void OnDisable()
    {
        #region TLP_DEBUG

#if TLP_DEBUG
        DebugLog(nameof(OnDisable));
#endif

        #endregion

        if (!Utilities.IsValid(LocalPlayerAdded) || !LocalPlayerAdded.RemoveListener(this, true))
        {
            ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerAdded)}");
        }

        if (!Utilities.IsValid(LocalPlayerRemoved) || !LocalPlayerRemoved.RemoveListener(this, true))
        {
            ErrorAndDisableComponent($"Failed to stop listening to {nameof(LocalPlayerRemoved)}");
        }
    }


    public override void OnEvent(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnLocalPlayerAdded):
                OnLocalPlayerAdded();
                break;
            case nameof(OnLocalPlayerRemoved):
                OnLocalPlayerRemoved();
                break;
            default:
                base.OnEvent(eventName);
                break;
        }
    }

    internal void OnLocalPlayerAdded()
    {
        #region TLP_DEBUG

#if TLP_DEBUG
        DebugLog(nameof(OnLocalPlayerAdded));
#endif

        #endregion

        if (Utilities.IsValid(OverrideWithDynamicPriority))
        {
            OverrideWithDynamicPriority.PrivacyChannelId = PlayerAddedPrivacyChannelId;
        }
    }

    internal void OnLocalPlayerRemoved()
    {
        #region TLP_DEBUG

#if TLP_DEBUG
        DebugLog(nameof(OnLocalPlayerRemoved));
#endif

        #endregion

        if (Utilities.IsValid(OverrideWithDynamicPriority))
        {
            OverrideWithDynamicPriority.PrivacyChannelId = PlayerExitedPrivacyChannelId;
        }
    }
}
}
