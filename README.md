# Timer - 赛事计时系统

[![CI/CD Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/workflows/CI/CD%20Pipeline/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions)
[![Release Build](https://github.com/YOUR_USERNAME/YOUR_REPO/workflows/Release%20Build/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

基于 WPF 和 .NET 10.0 的专业赛事计时管理系统，支持多组并行计时、芯片管理、人员分组等功能。

## ✨ 主要功能

### 🏃 比赛计时
- **多组并行计时**：同时管理多个比赛组，独立计时互不干扰
- **实时记录**：支持扫码/手动录入，实时记录每圈成绩
- **圈数配置**：在人员分组中统一配置圈数，计时页面自动应用
- **比赛状态管理**：开始、暂停、继续、停止，支持中断恢复

### 👥 人员管理
- **参赛人员管理**：导入/导出 Excel，批量管理参赛者信息
- **人员分组**：按学校、年级、班级、组别灵活分组
- **芯片分配**：为分组批量分配芯片，自动同步号码布

### 💳 芯片设备管理
- **芯片组管理**：创建芯片组，设置颜色标识
- **芯片导入**：支持 Excel 批量导入芯片信息
- **实时同步**：芯片组颜色修改后，全局实时更新

### 📊 成绩管理
- **成绩查询**：多维度筛选查看成绩
- **数据导出**：导出 Excel 报表

## 🛠️ 技术栈

- **框架**: .NET 10.0 + WPF
- **架构**: MVVM (使用 CommunityToolkit.Mvvm)
- **数据库**: SQLite (Microsoft.Data.Sqlite)
- **Excel 处理**: ClosedXML
- **消息通信**: WeakReferenceMessenger (实时数据同步)

## 📦 快速开始

### 环境要求

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 或 JetBrains Rider (可选)

### 克隆仓库

```bash
git clone https://github.com/YOUR_USERNAME/YOUR_REPO.git
cd YOUR_REPO
```

### 构建项目

#### 使用命令行

```bash
# 还原依赖
dotnet restore Timer/Timer/Timer.csproj

# Debug 构建
dotnet build Timer/Timer/Timer.csproj -c Debug

# Release 构建
dotnet build Timer/Timer/Timer.csproj -c Release

# 发布 (x64)
dotnet publish Timer/Timer/Timer.csproj -c Release -r win-x64 --self-contained false
```

#### 使用构建脚本

```bash
# Windows
build.bat
```

### 运行应用

```bash
# 方式 1: 直接运行
dotnet run --project Timer/Timer/Timer.csproj

# 方式 2: 运行构建产物
Timer\Timer\bin\Debug\net10.0-windows\Timer.exe
```

## 🚀 GitHub Actions CI/CD

本项目已配置自动化工作流：

### CI 流程 (`.github/workflows/ci.yml`)
- **触发条件**: Push 到 main/master/develop 分支，或创建 PR
- **执行任务**:
  - ✅ 代码检出
  - ✅ 还原 NuGet 依赖
  - ✅ Debug/Release 双模式编译
  - ✅ 上传构建产物（保留 7 天）
  - ✅ 代码格式检查

### Release 流程 (`.github/workflows/release.yml`)
- **触发条件**: 推送 `v*.*.*` 标签（如 `v1.0.3`）
- **执行任务**:
  - ✅ 发布 x64 版本
  - ✅ 压缩发布文件
  - ✅ 自动创建 GitHub Release
  - ✅ 上传构建产物（保留 30 天）

### PR 检查 (`.github/workflows/pr-checks.yml`)
- **触发条件**: 创建或更新 Pull Request
- **执行任务**:
  - ✅ 编译验证
  - ✅ XAML 语法检查
  - ✅ PR 大小检查
  - ✅ 自动添加检查结果评论

### 发布新版本

```bash
# 创建并推送标签
git tag v1.0.3
git push origin v1.0.3

# GitHub Actions 会自动:
# 1. 构建 Release 版本
# 2. 创建 GitHub Release
# 3. 上传构建产物
```

## 📁 项目结构

```
Timer/
├── Timer/
│   ├── ViewModels/          # MVVM 视图模型
│   ├── Views/               # XAML 视图
│   ├── Models/              # 数据模型
│   ├── Services/            # 业务逻辑服务
│   ├── Messages/            # 跨 VM 消息
│   ├── Converters/          # 值转换器
│   ├── Resources/           # 样式资源
│   └── Data/                # 数据库上下文
├── .github/
│   ├── workflows/           # GitHub Actions 工作流
│   ├── ISSUE_TEMPLATE/      # Issue 模板
│   └── dependabot.yml       # 依赖自动更新
├── build.bat                # Windows 构建脚本
├── setup.iss                # Inno Setup 安装脚本
└── README.md
```

## 🔧 配置说明

### 数据库位置
- **开发环境**: `Timer/Timer/bin/Debug/net10.0-windows/data/timer.db`
- **发布版本**: 与可执行文件同级的 `data/timer.db`

### Excel 模板
- **人员导入模板**: `人员导入模板.xls`
- **芯片导入**: 支持包含"标签号"和"内部号"列的 Excel 文件

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

### 代码规范
- 遵循 C# 编码规范
- 使用 MVVM 模式
- 添加必要的注释和文档
- 确保代码通过 CI 检查

## 📝 更新日志

### v1.0.2 (Latest)
- ✨ 将圈数配置移至人员分组，简化计时流程
- 🐛 修复芯片组颜色不同步问题
- 🔄 实现全局数据实时刷新
- 🎨 优化 DataGrid 对齐和样式

### v1.0.1
- 🐛 修复 XamlParseException
- 🔧 完善资源字典配置

### v1.0.0
- 🎉 首次发布

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM 框架
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) - Excel 处理
- [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) - SQLite 支持

## 📧 联系方式

如有问题或建议，请：
- 提交 [Issue](https://github.com/YOUR_USERNAME/YOUR_REPO/issues)
- 发送邮件至: your-email@example.com

---

⭐ 如果这个项目对你有帮助，请给个 Star！
