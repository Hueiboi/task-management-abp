using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class UserLookupDto : EntityDto<Guid>
{
    public string UserName { get; set; } = default!;
}