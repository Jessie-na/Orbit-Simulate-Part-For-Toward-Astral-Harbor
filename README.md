# Orbit Simulate Part For Toward Astral Harbor

这是一个为航天模拟游戏 **《Toward Astral Harbor》** 开发的高性能广义相对论（GR）轨道数值演算核心。本项目利用双精度浮点数、ADM 分解以及辛积分算法，在 Unity 中实现了对 Kerr 度规下测地线运动的精准模拟。

\---

## 物理与模拟原理

本项目的内核逻辑大量参考了 **Bacchini et al. (2018)** 的研究成果。

### 1\. 时空度规计算

系统通过 **3+1 分解 (ADM Decomposition)** 将四维时空线元 $ds^2$ 拆解，并在 `RelativityMath.cs` 中实时计算度规分量（基于 Boyer-Lindquist 坐标系

### 2\. 哈密顿演化方程

模拟的核心是将测地线方程转化为一阶哈密顿方程组。系统演化的是**协变动量 (Covariant Momentum $u\_i$)** 而非普通速度，其演化逻辑如下：

* **位置演化**：$\\frac{dx^i}{dt} = \\frac{\\gamma^{ij} u\_j}{u^0} - \\beta^i$
* **动量演化**：$\\frac{du\_i}{dt} = -\\alpha u^0 \\partial\_i \\alpha + u\_k \\partial\_i \\beta^k - \\frac{\\gamma^{jk} u\_j u\_k}{2 u^0} \\partial\_i \\gamma^{jk} + \\text{Newtonian Perturbations}$

其中 $u^0$ 为四维速度的时间分量

### 3\. 辛积分算法

为了保证轨道在长时间尺度或极高加速倍率下不发生由于数值误差导致的“伪能量漂移”，本项目采用了**辛积分器**：

* **IMR (Implicit Midpoint Rule)**：作为辛积分的一种，通过数值雅可比牛顿迭代求解。它具备无条件稳定性，即便在黑洞视界边缘等引力梯度极大的区域，也能精准保持系统的能量守恒。

\---

## 功能与操作

### 1\. 飞船控制系统

项目内置了符合相对论逻辑的飞船动力学模型，支持在赤道面上的 2D 自由移动：

* **滚转)**：`A` / `D` 控制飞船向左/向右旋转。
* **节流阀**：

  * `Left Shift` / `Left Ctrl`：渐进式 增加/减少 引擎推力。
  * `Z` / `X`：瞬间将节流阀调至 最大 (100%) / 关闭 (0%)。
* **相对论推力**：引擎产生的协变速度变化量会受当前时空 $u^0$ 的调制。

### 2\. 视角控制

* **旋转视角**：按住 `鼠标右键` 并拖动。
* **缩放视角**：滚动 `鼠标中键`。

### 3\. 可视化功能设置

* **轨道预测 (`GROrbitPrediction`)**：

  * 位于场景中的 **“太阳”** 或中心天体对象上。
  * **Enable Prediction**：勾选以开启 KSP 式的未来轨道预测。
  * **Predicted Points**：预测线的总采样点数。
  * **Prediction Time Step**：单个预测步长。
  * **Steps Per Frame**：分帧预测逻辑，设置每帧计算的步数以平摊 CPU 压力，防止卡顿。
* **参考系切换 (`GRWorldManager`)**：

  * 支持在不同天体间切换参考系（Reference Frame）。选择不同的GRPhysicsObject作为参考系，方便进行轨道转移。
* **相机追踪 (`CameraController`)**：

  * 位于 `CameraBaseRator` 对象上。
  * **Target**：将需要追踪的对象拖入槽位。

\---

## 场景说明 (Scenarios)

项目配置了四个预设场景，涵盖了从强场到弱场的多体模拟：

1. **PhysicsGR1 / 2 / 3**：

   * 广义相对论核心测试场景。包含不同旋转参数（$a$）的黑洞，用于观察近日点进动、光子轨道以及框架拖拽效应。
2. **SLIMSimulation**：

   * 弱场多体引力场景。模拟了日本 **SLIM 月球探测器** 的转移轨道。展示系统在处理远距离、多天体引力摄动下的数值稳定性。

\---

## 开发与集成

### 核心模块

* `RelativisticPhysics`: GR物理模拟的核心逻辑。
* `RelativityMath`: 存储度规张量计算公式。
* `FloatingPositionSystem`: 双精度浮点原点系统接口。
* `OrbitRendering`: 轨道可视化相关内容。

\---

