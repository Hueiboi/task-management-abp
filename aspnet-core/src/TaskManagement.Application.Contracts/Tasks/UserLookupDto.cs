using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class UserLookupDto : EntityDto<Guid>
{
    // NOTE: Hỗ trợ việc hiển thị tên người dùng trong các dropdown hoặc các phần liên quan đến người dùng khác
    public string UserName { get; set; } = default!;
}