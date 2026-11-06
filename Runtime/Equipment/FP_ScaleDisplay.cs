// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System.Globalization;
    using TMPro;
    using UnityEngine;

    /// <summary>Binds a scale to a Canvas or world-space TextMeshPro label.</summary>
    public class FP_ScaleDisplay : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentScale scale;
        [SerializeField] private TMP_Text weightLabel;
        [SerializeField, Range(0, 6)] private int decimalPlaces = 3;
        private FP_EquipmentScale _subscribedScale;

        private void OnEnable()
        {
            _subscribedScale = scale;
            if (_subscribedScale != null) _subscribedScale.OnStateChanged.AddListener(RefreshDisplay);
            RefreshDisplay(_subscribedScale);
        }

        private void OnDisable()
        {
            if (_subscribedScale != null) _subscribedScale.OnStateChanged.RemoveListener(RefreshDisplay);
            _subscribedScale = null;
        }

        public void RefreshDisplay(FP_EquipmentScale source)
        {
            if (weightLabel == null) return;
            weightLabel.text = source != null && source.IsReadingAvailable
                ? source.TotalWeightKg.ToString("F" + Mathf.Clamp(decimalPlaces, 0, 6), CultureInfo.InvariantCulture) + " kg"
                : "-- kg";
        }
    }
}
