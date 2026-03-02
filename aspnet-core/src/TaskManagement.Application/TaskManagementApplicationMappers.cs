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
        CreateMap<CreateUpdateTaskDto, AppTask>()
            .IgnoreFullAuditedObjectProperties(); // Bỏ qua các trường như CreationTime, CreatorId


        /* --- 2. MAPPING CHO PROJECT (DỰ ÁN) --- */

        // Ánh xạ từ Entity Project sang DTO
        CreateMap<Project, ProjectDto>();

        // Ánh xạ từ DTO sang Entity Project
        CreateMap<CreateUpdateProjectDto, Project>()
            .IgnoreFullAuditedObjectProperties()
            .ForMember(dest => dest.Members, opt => opt.Ignore()); // Bỏ qua danh sách Member để xử lý thủ công
    }
}