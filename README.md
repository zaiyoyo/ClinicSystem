# 门诊管理系统 API

## 技术栈
- ASP.NET Core 8 Web API
- Entity Framework Core + SQL Server
- JWT 认证授权
- Serilog 日志
- Swagger 文档
- Docker 部署

## 快速启动

### 开发环境

```bash
# 1. 确保已安装 .NET 8 SDK + SQL Server

# 2. 修改 appsettings.Development.json 中的连接字符串

# 3. 运行数据库迁移
cd src/ClinicSystem.Api
dotnet ef database update

# 4. 启动
dotnet run
# 访问 http://localhost:5000/swagger
```

### Docker 部署

```bash
docker-compose up -d
# 访问 http://localhost:5000/swagger
```

## 默认账号

| 用户名 | 密码 | 角色 |
|--------|------|------|
| admin | admin123 | 系统管理员 |

## 项目结构

```
ClinicSystem/
├── src/
│   ├── ClinicSystem.Api/          # Web API (Controllers, Program.cs)
│   ├── ClinicSystem.Application/  # 应用层 (DTOs, Interfaces)
│   ├── ClinicSystem.Domain/       # 领域层 (Entities, Enums)
│   └── ClinicSystem.Infrastructure/ # 基础设施 (DbContext, Repositories)
└── tests/
```

## API 列表

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | /api/auth/login | 登录获取 Token |
| GET  | /api/auth/me | 获取当前用户信息 |
| GET  | /api/users | 用户列表 |
| POST | /api/users | 创建用户 (Admin) |
| PUT  | /api/users/{id} | 更新用户 (Admin) |
| GET  | /api/departments | 科室列表 |
| POST | /api/departments | 创建科室 (Admin) |
| ...  | ... | ... |
