using System.Collections.Generic;
using System.Linq;
using Aloha.Dtos;
using Aloha.Helpers.FileHelper;
using Aloha.Mappers;
using Aloha.Model.Contexts;
using Aloha.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aloha.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class FloorsController : Controller
    {
        private readonly AlohaContext dbContext;
        private readonly IClassMapping<Floor, FloorDto> floorToFloorDtoMapping;
        private readonly IClassMapping<FloorDto, Floor> floorDtoToFloorMapping;
        private readonly IClassMapping<Workstation, WorkstationDto> workstationToWorkstationDtoMapping;
        private readonly IEntityUpdater<Floor> floorUpdater;

        public FloorsController(
            AlohaContext dbContext,
            IClassMapping<Floor, FloorDto> floorToFloorDtoMapping,
            IClassMapping<FloorDto, Floor> floorDtoToFloorMapping,
            IClassMapping<Workstation, WorkstationDto> workstationToWorkstationDtoMapping,
            IEntityUpdater<Floor> floorUpdater)
        {
            this.dbContext = dbContext;
            this.floorToFloorDtoMapping = floorToFloorDtoMapping;
            this.floorDtoToFloorMapping = floorDtoToFloorMapping;
            this.workstationToWorkstationDtoMapping = workstationToWorkstationDtoMapping;
            this.floorUpdater = floorUpdater;
        }

        [HttpGet]
        public List<FloorDto> List()
        {
            return dbContext.Set<Floor>()
                .Include(f => f.Office)
                .Include(f => f.Workstations)
                .Include(f => f.Image)
                .Select(floorToFloorDtoMapping.Map)
                .ToList();
        }

        [HttpGet("{id}")]
        public FloorDto Get(int id)
        {
            Floor floor = dbContext.Set<Floor>()
                .Include(f => f.Office)
                .Include(f => f.Workstations)
                .Include(f => f.Image)
                .Single(f => f.Id == id);

            return floor == null
                ? null
                : floorToFloorDtoMapping.Map(floor);
        }

        [HttpPost]
        public FloorDto Add([FromBody]FloorDto floorDto)
        {
            Floor floor = floorDtoToFloorMapping.Map(floorDto);

            Office office = dbContext.Offices
                .Include(o => o.Floors)
                .SingleOrDefault(o => o.Id == floorDto.OfficeId);

            office.Floors.Add(floor);

            dbContext.SaveChanges();

            return floorToFloorDtoMapping.Map(floor);
        }

        [HttpPut("{id}")]
        public FloorDto Update(int id, [FromBody]FloorDto floorDto)
        {
            Floor floor = floorDtoToFloorMapping.Map(floorDto);

            Floor actualFloor = dbContext.Floors
                .Include(f => f.Office)
                .Include(f => f.Workstations)
                .Include(f => f.Image)
                .SingleOrDefault(f => f.Id == id);

            if (floorDto.ImageUrl != null && floorDto.ImageUrl != string.Empty)
            {
                if (actualFloor.Image != null)
                {
                    dbContext.Remove(actualFloor.Image);
                }

                actualFloor.Image = FileHelper.GetFileFromBase64(floorDto.ImageUrl);
            }

            floorUpdater.Update(actualFloor, floor);

            dbContext.SaveChanges();

            return floorToFloorDtoMapping.Map(actualFloor);
        }

        [HttpDelete("{id}")]
        public void Remove(int id)
        {
            Floor floor = dbContext.Floors
                .Include(f => f.Image)
                .Single(f => f.Id == id);

            dbContext.Floors.Remove(floor);

            if (floor.Image != null)
            {
                dbContext.Files.Remove(floor.Image);
            }

            dbContext.SaveChanges();
        }
    }
}
