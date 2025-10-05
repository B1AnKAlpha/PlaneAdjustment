# PlaneADJ - 二维测量控制网平差

一个用于处理和平差二维测量控制网的 C#/.NET 桌面应用程序。本项目是西南交通大学测绘类专业的暑期实习设计。

![Windows Forms](https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows) ![.NET Framework](https://img.shields.io/badge/.NET-Framework-5C2D91?style=for-the-badge&logo=dotnet) ![C#](https://img.shields.io/badge/C%23-Language-239120?style=for-the-badge&logo=c-sharp)

---

## 功能特性

本应用程序提供了从数据输入到最终报告生成的完整平面网平差工作流程。

- **数据解析**：读取并解析标准 `.in2` 自由格式测量数据文件，自动识别已知点、观测值（角度和距离）以及特殊定向记录。
- **近似坐标计算**：使用导线计算方法计算未知点的初始坐标，支持复杂的网形配置。
- **最小二乘平差**：基于最小二乘原理实现严密的间接平差模型，计算精确的坐标改正数。
- **精度评定与报告**：
  - 生成详细的平差报告，包括标准偏差（`Mx`、`My`）、点位误差（`Mp`）以及后验单位权中误差（`σ₀`）。
  - 可视化控制网，区分已知点和未知点。
  - （可选）绘制点的**误差椭圆**，直观表示其定位精度。

---

## 界面截图

**主界面（数据与报告）**
![主应用程序窗口](./images/main.png)

**网形可视化**
![网形可视化](./images/img.png)

**数据库管理**
![数据库管理](./images/database.png)

---

## 快速开始

### 环境要求

- [Visual Studio](https://visualstudio.microsoft.com/)
- [.NET Framework](https://dotnet.microsoft.com/en-us/download/dotnet-framework)（项目文件中指定的版本）

### 安装步骤

1. **克隆仓库：**
   ```bash
   git clone https://github.com/B1AnKAlpha/PlaneAdjustment.git
   ```
2. **打开项目：**
   - 导航到 `Code/` 目录。
   - 使用 Visual Studio 打开 `.sln`（解决方案）或 `.csproj`（项目）文件。
3. **还原 NuGet 包：**
   - Visual Studio 应该会自动还原所需的包。如果没有，请在解决方案资源管理器中右键单击解决方案，然后选择"还原 NuGet 包"。
4. **运行应用程序：**
   - 按 `F5` 或单击 Visual Studio 中的"启动"按钮。

---

## 技术栈

- **语言**：C#
- **框架**：.NET Framework
- **用户界面**：Windows Forms (WinForms)
- **核心库**：
  - `MathNet.Numerics`：用于高性能矩阵计算。
  - `ScottPlot.WinForms`：用于交互式数据可视化和绘图。
