# CQRS 应用层指南（Core.Application）

> MediatR + FluentValidation 自动装配。命令 / 查询实现接口即被装配，校验器零注册、自动执行。

## 1. 引入

```csharp
// using Core.Modularity.Attribute;
// using Core.Application;

[DependsOn(typeof(CoreApplicationModule))]
public class StartupModule : CoreModuleBase
{
}
```

不走模块时：`services.AddApplication();`

## 2. 定义命令（Command）

命令实现 `ICommand` 接口，处理器实现 MediatR 的 `IRequestHandler<,>`：

```csharp
// using Core.Application.Commands;
// using MediatR;

public class CreateUserCommand : ICommand
{
    public string Name { get; set; }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Unit>
{
    public Task<Unit> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // 业务处理
        return Task.FromResult(Unit.Value);
    }
}
```

带返回结果的命令：`ICommand<out TResponse>`（约束 `TResponse : ICommandResult`），对应处理器 `IRequestHandler<CreateUserCommand, UserResult>`。

## 3. 定义查询（Query）

```csharp
// using Core.Application.Queries;
// using MediatR;

public class GetUserQuery : IQuery<UserResult> { public long Id { get; set; } }

public class UserResult : IQueryResult
{
    public string Name { get; set; }
}

public class GetUserHandler : IRequestHandler<GetUserQuery, UserResult>
{
    public Task<UserResult> Handle(GetUserQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new UserResult { Name = "a" });
}
```

分页查询：继承 `QueryPaging<TResponse>`（返回 `QueryPagedResult`）。

## 4. 校验（FluentValidation）

只需定义 `AbstractValidator<T>`，框架扫描全部非包程序集自动注册：

```csharp
// using FluentValidation;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

命令执行前由 `ValidationRequestPreProcessor` 自动执行校验，失败抛 `Core.Application.Exceptions.ValidationException`。

## 5. 分发

```csharp
// 注入 ISender / IMediator
await sender.Send(new CreateUserCommand { Name = "x" }, cancellationToken);
```

> `Core.Application` 内置 MediatR（11.1.0），`AddApplication()` 会注册所有请求处理器与校验器，无需逐个 `AddScoped`。
