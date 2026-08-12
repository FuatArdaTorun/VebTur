using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates the database with a fixed set of fictional demo hotels so search, filtering,
/// and recommendation logic can be demonstrated. All data here is fictional sample data.
/// </summary>
public static class HotelSeeder
{
    public static async Task SeedAsync(VebTurDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Hotels.AnyAsync(cancellationToken))
        {
            return;
        }

        var amenities = BuildAmenities();
        db.Amenities.AddRange(amenities.Values);

        var hotels = BuildHotels(amenities);
        db.Hotels.AddRange(hotels);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, Amenity> BuildAmenities()
    {
        (string Slug, string Name, string IconKey)[] definitions =
        [
            ("pool", "Swimming Pool", "pool"),
            ("spa", "Spa & Wellness", "spa"),
            ("parking", "Free Parking", "parking"),
            ("wifi", "Free Wi-Fi", "wifi"),
            ("breakfast", "Breakfast Included", "breakfast"),
            ("gym", "Fitness Center", "gym"),
            ("family-friendly", "Family Friendly", "family"),
            ("beach-access", "Private Beach Access", "beach"),
            ("air-conditioning", "Air Conditioning", "ac"),
            ("bar", "Bar / Lounge", "bar"),
            ("sea-view", "Sea View", "sea-view"),
            ("pet-friendly", "Pet Friendly", "pet"),
        ];

        return definitions.ToDictionary(
            d => d.Slug,
            d => new Amenity { Name = d.Name, Slug = d.Slug, IconKey = d.IconKey });
    }

    private static List<Hotel> BuildHotels(Dictionary<string, Amenity> amenities)
    {
        List<Hotel> hotels = [];

        hotels.Add(Build(amenities,
            name: "Antalya Sunset Resort & Spa", slug: "antalya-sunset-resort-spa",
            city: "Antalya", lat: 36.8560, lon: 30.7350, star: 5,
            description: "A beachfront resort on Lara's coastline offering panoramic Mediterranean sunsets, a full-service spa, and family-friendly pools just minutes from Antalya's old town.",
            amenitySlugs: ["pool", "spa", "beach-access", "wifi", "breakfast", "family-friendly", "air-conditioning"],
            rooms: [("Standard Room", 2, 3200m), ("Deluxe Sea View", 2, 4800m), ("Family Suite", 4, 7200m)],
            supervisorName: "Elif Aydın"));

        hotels.Add(Build(amenities,
            name: "Lara Beach Palace Hotel", slug: "lara-beach-palace-hotel",
            city: "Antalya", lat: 36.8402, lon: 30.7897, star: 5,
            description: "An all-inclusive palace-style hotel on Lara Beach with landscaped gardens, multiple pools, and direct beach access for a classic Antalya holiday.",
            amenitySlugs: ["pool", "beach-access", "spa", "gym", "bar", "wifi", "breakfast"],
            rooms: [("Standard Room", 2, 3600m), ("Deluxe Room", 3, 5100m), ("Presidential Suite", 4, 9800m)],
            supervisorName: "Murat Şahin"));

        hotels.Add(Build(amenities,
            name: "Kaleiçi Boutique Hotel", slug: "kaleici-boutique-hotel",
            city: "Antalya", lat: 36.8850, lon: 30.7050, star: 4,
            description: "A restored Ottoman-era house in Antalya's historic Kaleiçi quarter, steps from the marina, offering a quiet boutique stay with a rooftop terrace.",
            amenitySlugs: ["wifi", "breakfast", "air-conditioning", "pet-friendly"],
            rooms: [("Standard Room", 2, 2400m), ("Terrace Room", 2, 3100m)],
            supervisorName: "Zeynep Kaya"));

        hotels.Add(Build(amenities,
            name: "Bosphorus View Hotel", slug: "bosphorus-view-hotel",
            city: "Istanbul", lat: 41.0430, lon: 29.0094, star: 5,
            description: "A waterfront hotel with unobstructed Bosphorus views, located in Ortaköy within walking distance of cafes, mosques, and the strait's ferry piers.",
            amenitySlugs: ["wifi", "breakfast", "gym", "bar", "sea-view", "air-conditioning"],
            rooms: [("Standard Room", 2, 4200m), ("Bosphorus View Room", 2, 6500m), ("Executive Suite", 3, 9900m)],
            supervisorName: "Can Demir"));

        hotels.Add(Build(amenities,
            name: "Sultanahmet Heritage Hotel", slug: "sultanahmet-heritage-hotel",
            city: "Istanbul", lat: 41.0058, lon: 28.9769, star: 4,
            description: "A heritage building steps from the Blue Mosque and Hagia Sophia, blending Ottoman architecture with modern comfort for exploring Istanbul's old city.",
            amenitySlugs: ["wifi", "breakfast", "air-conditioning", "family-friendly"],
            rooms: [("Standard Room", 2, 3300m), ("Family Room", 4, 5200m)],
            supervisorName: "Ayşe Yıldız"));

        hotels.Add(Build(amenities,
            name: "Taksim City Suites", slug: "taksim-city-suites",
            city: "Istanbul", lat: 41.0370, lon: 28.9850, star: 4,
            description: "Modern serviced suites in the heart of Taksim, close to İstiklal Street's shopping and nightlife, ideal for business and city-break travelers.",
            amenitySlugs: ["wifi", "gym", "parking", "air-conditioning", "bar"],
            rooms: [("Studio Suite", 2, 2900m), ("One-Bedroom Suite", 3, 4100m)],
            supervisorName: "Burak Öztürk"));

        hotels.Add(Build(amenities,
            name: "Izmir Bay Hotel", slug: "izmir-bay-hotel",
            city: "Izmir", lat: 38.4394, lon: 27.1287, star: 4,
            description: "A modern hotel overlooking Izmir Bay along the Kordon promenade, close to the city's seaside cafes and the historic Konak Square.",
            amenitySlugs: ["wifi", "breakfast", "gym", "sea-view", "air-conditioning"],
            rooms: [("Standard Room", 2, 2600m), ("Bay View Room", 2, 3400m)],
            supervisorName: "Deniz Arslan"));

        hotels.Add(Build(amenities,
            name: "Alsancak Marina Hotel", slug: "alsancak-marina-hotel",
            city: "Izmir", lat: 38.4368, lon: 27.1428, star: 3,
            description: "A comfortable mid-range hotel in Izmir's lively Alsancak district, close to the marina, restaurants, and nightlife.",
            amenitySlugs: ["wifi", "breakfast", "parking", "pet-friendly"],
            rooms: [("Standard Room", 2, 1900m), ("Superior Room", 2, 2500m)],
            supervisorName: "Selin Koç"));

        hotels.Add(Build(amenities,
            name: "Bodrum Marina Resort", slug: "bodrum-marina-resort",
            city: "Bodrum", lat: 37.0344, lon: 27.4305, star: 5,
            description: "A resort overlooking Bodrum's yacht marina with infinity pools, a full spa, and easy access to the town's castle and boutique shops.",
            amenitySlugs: ["pool", "spa", "beach-access", "sea-view", "wifi", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 4400m), ("Marina View Suite", 3, 7600m), ("Family Villa", 5, 11500m)],
            supervisorName: "Emre Yavuz"));

        hotels.Add(Build(amenities,
            name: "Gümbet Beach Hotel", slug: "gumbet-beach-hotel",
            city: "Bodrum", lat: 37.0300, lon: 27.4028, star: 3,
            description: "A lively beachfront hotel in Gümbet with direct beach access, popular with younger travelers for its nightlife and water sports nearby.",
            amenitySlugs: ["pool", "beach-access", "wifi", "bar", "family-friendly"],
            rooms: [("Standard Room", 2, 2100m), ("Sea View Room", 3, 2900m)],
            supervisorName: "Gizem Er"));

        hotels.Add(Build(amenities,
            name: "Ölüdeniz Lagoon Resort", slug: "oludeniz-lagoon-resort",
            city: "Fethiye", lat: 36.5497, lon: 29.1153, star: 5,
            description: "Set above the turquoise waters of the Blue Lagoon, this resort offers panoramic views, an infinity pool, and easy access to paragliding launch points.",
            amenitySlugs: ["pool", "spa", "beach-access", "sea-view", "wifi", "breakfast"],
            rooms: [("Standard Room", 2, 3800m), ("Lagoon View Suite", 3, 6200m)],
            supervisorName: "Onur Polat"));

        hotels.Add(Build(amenities,
            name: "Fethiye Harbor Hotel", slug: "fethiye-harbor-hotel",
            city: "Fethiye", lat: 36.6212, lon: 29.1164, star: 4,
            description: "A harborside hotel in central Fethiye with views over the marina, close to the Fish Market and the town's Friday bazaar.",
            amenitySlugs: ["wifi", "breakfast", "parking", "pet-friendly", "air-conditioning"],
            rooms: [("Standard Room", 2, 2700m), ("Harbor View Room", 2, 3500m)],
            supervisorName: "Pınar Uçar"));

        hotels.Add(Build(amenities,
            name: "Cappadocia Cave Suites", slug: "cappadocia-cave-suites",
            city: "Cappadocia", lat: 38.6431, lon: 34.8289, star: 5,
            description: "Hand-carved cave rooms in Göreme with terraces overlooking the valley — a signature Cappadocia stay, with hot-air balloons visible at sunrise.",
            amenitySlugs: ["wifi", "breakfast", "spa", "family-friendly"],
            rooms: [("Cave Room", 2, 4600m), ("Terrace Cave Suite", 3, 6800m)],
            supervisorName: "Kerem Bulut"));

        hotels.Add(Build(amenities,
            name: "Göreme Panorama Hotel", slug: "goreme-panorama-hotel",
            city: "Cappadocia", lat: 38.6428, lon: 34.8286, star: 4,
            description: "A valley-view hotel in the center of Göreme with a terrace restaurant overlooking the fairy chimneys, close to open-air museum tours.",
            amenitySlugs: ["wifi", "breakfast", "parking", "air-conditioning"],
            rooms: [("Standard Room", 2, 2800m), ("Panorama Room", 2, 3600m)],
            supervisorName: "Nur Aksoy"));

        return hotels;
    }

    private static readonly Dictionary<string, string> DistrictByCity = new()
    {
        ["Antalya"] = "Lara District",
        ["Istanbul"] = "Beyoğlu District",
        ["Izmir"] = "Alsancak District",
        ["Bodrum"] = "Bodrum Marina District",
        ["Fethiye"] = "Fethiye Harbor District",
        ["Cappadocia"] = "Göreme District",
    };

    private static Hotel Build(
        Dictionary<string, Amenity> amenities,
        string name,
        string slug,
        string city,
        double lat,
        double lon,
        int star,
        string description,
        string[] amenitySlugs,
        (string Name, int Capacity, decimal Price)[] rooms,
        string supervisorName)
    {
        var hotel = new Hotel
        {
            Name = name,
            Slug = slug,
            Description = description,
            City = city,
            Country = "Turkey",
            Address = DistrictByCity[city],
            Latitude = lat,
            Longitude = lon,
            StarRating = star,
        };

        hotel.Images = Enumerable.Range(1, 4)
            .Select(n => new HotelImage
            {
                HotelId = hotel.Id,
                Url = $"https://picsum.photos/seed/vebtur-{slug}-{n}/1200/800",
                AltText = $"{name} — photo {n}",
                DisplayOrder = n,
            })
            .ToList();

        hotel.RoomTypes = rooms
            .Select(r => new RoomType
            {
                HotelId = hotel.Id,
                Name = r.Name,
                Description = $"{r.Name} for up to {r.Capacity} guests.",
                Capacity = r.Capacity,
                BaseNightlyPrice = r.Price,
                Currency = "TRY",
            })
            .ToList();

        hotel.Supervisors =
        [
            new HotelSupervisor
            {
                HotelId = hotel.Id,
                FullName = supervisorName,
                Email = $"reservations@{slug}.example",
            }
        ];

        hotel.HotelAmenities = amenitySlugs
            .Select(s => new HotelAmenity { HotelId = hotel.Id, AmenityId = amenities[s].Id })
            .ToList();

        return hotel;
    }
}
