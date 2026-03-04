using AutoMapper;
using TaskManagement.Tasks;
using TaskManagement.Projects; 
using Volo.Abp.AutoMapper;

namespace TaskManagement;

public class TaskManagementApplicationAutoMapperProfile : Profile
{
    public TaskManagementApplicationAutoMapperProfile()
    {
        /* --- 1. MAPPING CHO TASK (CÔNG VIỆC) --- */
        
        // Ánh xạ từ Entity sang DTO để trả về dữ liệu cho FE
        CreateMap<AppTask, TaskDto>();
        
        // Ánh xạ từ DTO sang Entity để lưu xuống Database
        // Lấy dữ liệu từ FE không cần các trường như CreationTime, CreatorId -> ABP tự lo
        CreateMap<CreateUpdateTaskDto, AppTask>()
            .IgnoreFullAuditedObjectProperties();


        /* --- 2. MAPPING CHO PROJECT (DỰ ÁN) --- */

        // Ánh xạ từ Entity Project sang DTO
        CreateMap<Project, ProjectDto>();

        // Ánh xạ từ DTO sang Entity Project
        // Bỏ qua danh sách Member để xử lý thủ công
        // `Members` là collection phức tạp, AutoMapper không biết cách map
        // Code trong `UpdateAsync` đã xử lý thủ công `Clear()` + `Add()` rồi
        CreateMap<CreateUpdateProjectDto, Project>()
            .IgnoreFullAuditedObjectProperties()
            .ForMember(dest => dest.Members, opt => opt.Ignore()); 
    }
}