using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Kaligo.Services;
using Kaligo.UI;

namespace Kaligo.Items
{
    /// <summary>
    /// Attached to a dead enemy by EnemyAI on death.
    ///
    /// Interaction rules:
    ///   • Prompt appears only for the closest body the camera crosshair is
    ///     aimed at, within interactRange. Two corpses never prompt at once.
    ///   • [F] opens/loots the aimed-at body.
    ///   • Left-click opens — but only in UI mode (Alt key). In action-camera
    ///     mode clicks are ignored so accidental opens don't happen mid-combat.
    /// </summary>
    public class LootContainer : MonoBehaviour
    {
        // ── Static ────────────────────────────────────────────────────────────
        public static LootContainer ActiveContainer { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────
        private readonly List<(ItemData item, int qty)> _loot = new();
        public  IReadOnlyList<(ItemData item, int qty)> Loot  => _loot;

        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private float aimRange      = 8f;
        private const float AimSphereRadius = 0.18f;   // sphere-cast radius — forgives slight off-centre aim

        private Transform       _player;
        private Vector3         _anchorPos;
        private bool            _playerInRange;

        public bool IsOpen { get; private set; }
        public event Action OnLootChanged;

        private GameObject      _promptGO;
        private TextMeshPro     _promptTMP;
        private CapsuleCollider _col;

        // ── Initialization ────────────────────────────────────────────────────

        public void Initialize(List<(ItemData, int)> drops)
        {
            _loot.AddRange(drops);
            _anchorPos = transform.position;

            _col        = gameObject.AddComponent<CapsuleCollider>();
            _col.center = new Vector3(0f, 0.9f, 0f);
            _col.radius = 0.45f;
            _col.height = 1.8f;

            CreatePrompt();
        }

        private void Awake()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) _player = p.transform;
                else return;
            }

            // Proximity — XZ only (body height irrelevant)
            float horizDist = new Vector2(
                _anchorPos.x - _player.position.x,
                _anchorPos.z - _player.position.z).magnitude;
            bool inRange = horizDist <= interactRange;

            if (inRange != _playerInRange)
            {
                _playerInRange = inRange;
                if (!inRange && IsOpen) CloseWindow();
            }

            // Aim — checked every frame; only closest aimed container wins
            bool aimed = inRange && IsClosestAimed();

            // Active-container ownership:
            //   • Claim it when aimed at.
            //   • Keep it while the window is open (so no other body steals focus).
            //   • Release only when neither aimed nor open.
            if (aimed || IsOpen)
            {
                ActiveContainer = this;
                ShowPrompt(aimed && !IsOpen);   // hide prompt while window is visible
            }
            else if (ActiveContainer == this)
            {
                ActiveContainer = null;
                ShowPrompt(false);
            }

            // ── [F] key ───────────────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!IsOpen && aimed)
                    OpenWindow();
                else if (IsOpen && ActiveContainer == this)
                {
                    if (_loot.Count > 0) LootAll();
                    else                 CloseWindow();
                }
            }

            // ── Left-click — UI / cursor mode only ────────────────────────────
            // CursorController.IsUIMode is true when the player pressed Alt to
            // free the cursor; false in normal action-camera mode.
            bool uiMode = CursorController.Instance != null
                          ? CursorController.Instance.IsUIMode
                          : Cursor.lockState == CursorLockMode.None;

            if (!IsOpen && uiMode && Input.GetMouseButtonDown(0) && Camera.main != null)
            {
                var ray  = Camera.main.ScreenPointToRay(Input.mousePosition);
                var hits = Physics.RaycastAll(ray, aimRange);
                foreach (var h in hits)
                    if (h.collider == _col) { OpenWindow(); break; }
            }

            // ── Billboard prompt ──────────────────────────────────────────────
            if (_promptGO != null && _promptGO.activeSelf && Camera.main != null)
            {
                _promptGO.transform.position = _anchorPos + Vector3.up * 0.9f;
                _promptGO.transform.rotation = Camera.main.transform.rotation;
            }
        }

        // ── Aim helper ────────────────────────────────────────────────────────

        /// <summary>
        /// True when this container is the nearest loot body hit by a thin
        /// sphere-cast along the camera's forward. Using SphereCastAll instead
        /// of RaycastAll gives a small aim forgiveness so the player doesn't
        /// have to pixel-perfectly centre the crosshair.
        /// </summary>
        private bool IsClosestAimed()
        {
            if (Camera.main == null) return false;

            var origin    = Camera.main.transform.position;
            var direction = Camera.main.transform.forward;

            float         closest   = float.MaxValue;
            LootContainer winner    = null;

            foreach (var h in Physics.SphereCastAll(origin, AimSphereRadius, direction, aimRange))
            {
                var lc = h.collider.GetComponentInParent<LootContainer>();
                if (lc == null) continue;
                if (h.distance < closest) { closest = h.distance; winner = lc; }
            }

            return winner == this;
        }

        // ── Public loot API ───────────────────────────────────────────────────

        public void TakeItem(int index)
        {
            if (index < 0 || index >= _loot.Count) return;
            var (item, qty) = _loot[index];
            GameServices.Inventory?.Add(item.itemId, qty);
            SpawnFeedback(item, qty);
            _loot.RemoveAt(index);
            if (_loot.Count == 0) FinishLooting();
            else                  OnLootChanged?.Invoke();
        }

        public void LootAll()
        {
            foreach (var (item, qty) in _loot)
            {
                GameServices.Inventory?.Add(item.itemId, qty);
                SpawnFeedback(item, qty);
            }
            _loot.Clear();
            FinishLooting();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void OpenWindow()
        {
            IsOpen = true;
            ShowPrompt(false);
            LootWindowUI.Open(this);
        }

        private void CloseWindow()
        {
            IsOpen = false;
            LootWindowUI.Close();
        }

        private void FinishLooting()
        {
            IsOpen = false;
            if (ActiveContainer == this) ActiveContainer = null;
            LootWindowUI.Close();
            ShowPrompt(false);
            if (_col != null) Destroy(_col);
            StartCoroutine(FadeAndDestroy(1.5f));
        }

        private IEnumerator FadeAndDestroy(float delay)
        {
            yield return new WaitForSeconds(delay);

            var renderers = GetComponentsInChildren<Renderer>();
            const float fadeDuration = 1f;
            float t = 0f;

            foreach (var r in renderers)
                foreach (var mat in r.materials)
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.renderQueue = 3000;
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float alpha = 1f - t / fadeDuration;
                foreach (var r in renderers)
                    foreach (var mat in r.materials)
                        if (mat.HasProperty("_BaseColor"))
                        {
                            var c = mat.GetColor("_BaseColor");
                            c.a = alpha;
                            mat.SetColor("_BaseColor", c);
                        }
                yield return null;
            }

            Destroy(gameObject);
        }

        private void CreatePrompt()
        {
            _promptGO = new GameObject("LootPrompt");
            _promptGO.transform.position = _anchorPos + Vector3.up * 0.9f;

            _promptTMP              = _promptGO.AddComponent<TextMeshPro>();
            _promptTMP.text         = "[F] Loot";
            _promptTMP.fontSize     = 4f;
            _promptTMP.alignment    = TextAlignmentOptions.Center;
            _promptTMP.color        = new Color(1f, 0.92f, 0.35f);
            _promptTMP.fontStyle    = FontStyles.Bold;
            _promptTMP.sortingOrder = 10;

            _promptGO.SetActive(false);
        }

        private void ShowPrompt(bool visible)
        {
            if (_promptGO != null && _promptGO.activeSelf != visible)
                _promptGO.SetActive(visible);
        }

        private void SpawnFeedback(ItemData item, int qty)
        {
            var go  = new GameObject("LootFeedback");
            var tmp = go.AddComponent<TextMeshPro>();
            string q = qty > 1 ? $" x{qty}" : "";
            tmp.text         = $"+{item.displayName}{q}";
            tmp.color        = item.RarityColor();
            tmp.fontSize     = 3.5f;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.sortingOrder = 10;
            go.transform.position = _anchorPos + Vector3.up * 1.8f;
            go.AddComponent<LootFeedbackFloat>();
        }

        private void OnDestroy()
        {
            if (ActiveContainer == this) ActiveContainer = null;
            if (_promptGO != null) Destroy(_promptGO);
        }
    }

    // ── Floating feedback text ─────────────────────────────────────────────────

    internal class LootFeedbackFloat : MonoBehaviour
    {
        private TextMeshPro _tmp;
        private float       _t;
        private const float Duration = 1.5f;
        private const float Speed    = 1.2f;

        private void Awake() => _tmp = GetComponent<TextMeshPro>();

        private void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * Speed * Time.deltaTime;
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;
            float alpha = 1f - _t / Duration;
            _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, Mathf.Clamp01(alpha));
            if (_t >= Duration) Destroy(gameObject);
        }
    }
}
