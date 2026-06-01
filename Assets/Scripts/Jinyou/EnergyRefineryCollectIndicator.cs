using UnityEngine;
using UnityEngine.UI;

public class EnergyRefineryCollectIndicator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BaseCampManager baseCampManager;
    [SerializeField] private EnergyRefinery energyRefinery;

    [Header("Button")]
    [SerializeField] private Button collectButton;
    [SerializeField] private bool hideWhenNotFull = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        collectButton?.onClick.AddListener(CollectCredits);
        Refresh();
    }

    private void OnDisable()
    {
        collectButton?.onClick.RemoveListener(CollectCredits);
    }

    private void Update()
    {
        Refresh();
    }

    public void Configure(BaseCampManager manager, EnergyRefinery refinery, Button button)
    {
        baseCampManager = manager;
        energyRefinery = refinery;
        collectButton = button;
        Refresh();
    }

    private void CollectCredits()
    {
        if (energyRefinery == null || !energyRefinery.IsStorageFull)
        {
            return;
        }

        baseCampManager?.CollectRefineryCredits();
        Refresh();
    }

    private void Refresh()
    {
        ResolveReferences();

        bool canCollect = energyRefinery != null && energyRefinery.IsStorageFull;

        if (collectButton != null)
        {
            collectButton.interactable = canCollect;

            if (hideWhenNotFull && collectButton.gameObject != gameObject)
            {
                collectButton.gameObject.SetActive(canCollect);
            }
        }
    }

    private void ResolveReferences()
    {
        baseCampManager ??= BaseCampManager.Instance ?? FindFirstObjectByType<BaseCampManager>();
        energyRefinery ??= baseCampManager != null ? baseCampManager.EnergyRefinery : null;
        collectButton ??= GetComponent<Button>() ?? GetComponentInChildren<Button>(true);
    }
}
