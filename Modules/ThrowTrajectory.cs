using FullBrightMod.Core;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 投掷物抛物线预测模块。
    ///
    /// 原版分析结论：
    ///   - 投掷键：KeyBinds.GetBind("throw")，默认 T 键
    ///   - 蓄力计时：PlayerCamera.timeHeldThrow（按 T 时每帧 += Time.deltaTime * 0.8f，松键归零）
    ///   - 投掷触发：timeHeldThrow >= 0.15f 时调用 body.ThrowItem(timeHeldThrow * 2f)
    ///   - 初始速度：body.ThrowVelocity(item, force)
    ///       = (targetLookPos - item.transform.position).normalized
    ///         * actualJumpSpeed * 2.5f
    ///         * (1f + skills.STRFrom10 * 0.1f)
    ///         * (force / Mathf.Lerp(item.totalWeight, 1f, 0.6f))
    ///         + rb.velocity
    ///   - 原版自身有 HandleThrowLine() 用 LineRenderer 画抛物线，但无碰撞检测
    ///   - 重力使用 Physics2D.gravity * rb.gravityScale（投掷时 gravityScale 通常为 1）
    ///
    /// 本模块使用 OnGUI + 物理步进模拟，包含 Ground 碰撞检测和落点标记。
    /// </summary>
    public class ThrowTrajectory : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_trajectory");
        public override string Description => "Predictive throw trajectory with collision detection.";
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable() => ModSettings.IsTrajectoryEnabled = true;
        public override void OnDisable() => ModSettings.IsTrajectoryEnabled = false;

        // 物理模拟参数
        private const float TimeStep = 0.05f;
        private const int MaxSteps = 100;
        private static readonly int GroundMask = LayerMask.GetMask("Ground");

        public override float GetSettingsHeight()
        {
            // 颜色选择器行 + 滑块行
            return 30f + 30f + 10f; // padding
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            CustomGUI.DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_trajectory_color") + ":",
                ref ModSettings.TrajectoryColor);
            y += 8f;

            // 最大模拟距离滑块
            float val = ModSettings.TrajectoryMaxLength;
            CustomGUI.DrawSlider(x, ref y, width, e,
                Utils.I18n.Get("set_trajectory_length") + $": {val:F0}m",
                ref val, 5f, 80f);
            ModSettings.TrajectoryMaxLength = val;
        }

        public override void OnGUI()
        {
            if (!ModSettings.IsTrajectoryEnabled) return;

            // ---- 安全校验 ----
            if (PlayerCamera.main == null) return;
            var body = PlayerCamera.main.body;
            if (body == null || !body.conscious) return;
            var cam = Camera.main;
            if (cam == null) return;

            // 获取手持物品
            int handSlot = body.handSlot;
            Item heldItem = body.GetItem(handSlot);
            if (heldItem == null) return;

            // ---- 核心条件：按下投掷键且蓄力超过阈值 ----
            // 原版逻辑：Input.GetKey(KeyBinds.GetBind("throw")) 时蓄力
            // PlayerCamera.main.timeHeldThrow >= 0.15f 时显示抛物线
            if (PlayerCamera.main.timeHeldThrow < 0.05f)
                return;

            // ---- 计算初始速度（完全复刻原版 Body.ThrowVelocity） ----
            float force = Mathf.Clamp01(PlayerCamera.main.timeHeldThrow * 2f);
            Vector2 initVel = body.ThrowVelocity(heldItem, force);

            // ---- 物理步进模拟 ----
            Vector2 pos = body.slots[handSlot].transform.position; // 投掷起点 = 手部插槽
            Vector2 vel = initVel;

            // 物品的 gravityScale：投掷物品通常为 1（没有特殊组件则默认）
            float gravityScale = 1f;
            if (heldItem.TryGetComponent<Rigidbody2D>(out var itemRb))
                gravityScale = itemRb.gravityScale;

            Vector2 gravity = Physics2D.gravity * gravityScale;

            float totalDist = 0f;
            float maxDist = ModSettings.TrajectoryMaxLength;

            for (int i = 0; i < MaxSteps; i++)
            {
                Vector2 nextPos = pos + vel * TimeStep;
                float segmentLen = Vector2.Distance(nextPos, pos);
                totalDist += segmentLen;

                // 超过最大距离则终止
                if (totalDist > maxDist)
                {
                    // 绘制截断前最后一段
                    RenderUtils.DrawWorldLine(cam, pos, nextPos, ModSettings.TrajectoryColor);
                    break;
                }

                // 碰撞检测
                RaycastHit2D hit = Physics2D.Linecast(pos, nextPos, GroundMask);
                if (hit.collider != null)
                {
                    // 在线段上找到碰撞点，绘制直到碰撞点的线段
                    RenderUtils.DrawWorldLine(cam, pos, hit.point, ModSettings.TrajectoryColor);
                    // 落点画圈
                    RenderUtils.DrawWorldCircle(cam, hit.point, 0.3f, ModSettings.TrajectoryColor);
                    break;
                }

                // 绘制当前线段
                RenderUtils.DrawWorldLine(cam, pos, nextPos, ModSettings.TrajectoryColor);

                // 更新物理状态
                vel += gravity * TimeStep;
                pos = nextPos;

                // 最后一个步进完成且未碰撞也没超距：在终点画圈标记
                if (i == MaxSteps - 1)
                    RenderUtils.DrawWorldCircle(cam, pos, 0.3f, ModSettings.TrajectoryColor);
            }
        }
    }
}
