﻿using MyProject.Application.TodoLists.Commands.CreateTodoList;
using MyProject.Application.TodoLists.Commands.DeleteTodoList;
using MyProject.Domain.Entities;

namespace MyProject.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(99);
        await Should.ThrowAsync<NotFoundException>(() => SendAsync(command));
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await SendAsync(new DeleteTodoListCommand(listId));

        var list = await FindAsync<TodoList>(listId);

        list.ShouldBeNull();
    }
}
