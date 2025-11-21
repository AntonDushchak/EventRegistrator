using EventRegistrator.Application.Enums;
using EventRegistrator.Application.States;

namespace EventRegistrator.Application.Interfaces
{
    public interface IMenuService
    {
        Menu Get(MenuKey key, MenuContext ctx);
    }

}
