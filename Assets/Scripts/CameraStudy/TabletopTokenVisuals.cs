using System.Collections.Generic;
using UnityEngine;

/// <summary>Подгоняет фишки караванов под масштаб доски, не меняя правила движения.</summary>
[DefaultExecutionOrder(200)]
public sealed class TabletopTokenVisuals : MonoBehaviour
{
    [SerializeField] private PlayerToken _player;
    [SerializeField] private AITurnManager _aiTurns;
    [SerializeField] private Mesh _pawnMesh;
    [SerializeField] private Material _playerMaterial;
    [SerializeField] private Material _aiMaterial;
    private readonly HashSet<AITrader> _configured = new();

    private void Start() => AddPawn(_player.gameObject, _playerMaterial);

    private void LateUpdate()
    {
        foreach (var trader in _aiTurns.AiTraders)
        {
            if (trader == null || !_configured.Add(trader)) continue;
            AddPawn(trader.gameObject, _aiMaterial);
            if (trader.CurrentCity != null && !trader.GetComponent<PathController>().HasActivePath)
                trader.transform.position = trader.CurrentCity.transform.position;
        }
    }

    private void AddPawn(GameObject token, Material material)
    {
        foreach (var renderer in token.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var collider in token.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        token.transform.localScale = Vector3.one;
        var pawn = new GameObject("CaravanPawn", typeof(MeshFilter), typeof(MeshRenderer));
        pawn.transform.SetParent(token.transform, false);
        pawn.transform.localPosition = Vector3.up * .45f;
        pawn.transform.localScale = new Vector3(.3f, .4f, .3f);
        pawn.GetComponent<MeshFilter>().sharedMesh = _pawnMesh;
        pawn.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
