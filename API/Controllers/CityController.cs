using API.Data;
using Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public record CityModel(string Name); 
    
    [Route("api/cities")]
    [ApiController]
    public class CityController(AppDbContext _context, ILogger<CityController> _logger) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<City>>> Get()
        {
            var cities = await _context.Cities.ToListAsync();
            return cities;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<City?>> Get(int id)
        {
            var city = await _context.Cities.Where(x => x.Id == id).FirstOrDefaultAsync();
            return city;
        }

        [HttpPost]
        public async Task<ActionResult<City>> Post(CityModel model)
        {
            City city = new() {Name = model.Name};
            _context.Cities.Add(city);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = city.Id }, city);
        }

        [HttpPut]
        public async Task<ActionResult<City>> Put(City city)
        {
            _context.Entry(city).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest();
            }

            return city;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<City>> Delete(int id)
        {
            var city = await _context.Cities.FindAsync(id);

            if (city is null) 
            {
                return NotFound();
            }

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
