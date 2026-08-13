using VebTur.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace VebTur.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates the database with real hotels — clients of the hotel-ERP company this project's
/// author interns at (see https://www.veboni.com/tr/referanslarimiz/). Name, official website,
/// address, star rating, description, photos, and phone number are sourced from each hotel's
/// own official website; nothing here is AI-invented. Star ratings are null where the hotel's
/// own site does not state one — never guessed (re-verified 2026-08-12: several previously-set
/// ratings turned out unconfirmed on the hotel's own site and were cleared back to null; only
/// ratings whose exact text appears on the hotel's own page are kept). GoogleRating/GoogleRatingCount
/// are each hotel's real Google Maps rating, manually captured 2026-08-12 (see Hotel.GoogleRating
/// for why this isn't live API data yet) — never fabricated, never sourced from review text.
/// Room types/prices remain demo/estimated values since there is no live rate integration.
/// </summary>
public static class HotelSeeder
{
    private static readonly DateTime GoogleRatingCapturedAt = new(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);

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
            name: "Crystal Waterworld Aqua Collection", slug: "crystal-waterworld-aqua-collection",
            website: "https://crystalhotels.com.tr/en/hotels/crystal-waterworld",
            city: "Antalya", address: "Boğazkent Mah., 33. Sok. No:2, Serik (Belek)",
            lat: 36.850, lon: 31.052, star: null,
            googleRating: 4.6m, googleRatingCount: null,
            phone: "+90 242 777 0 777",
            description: "A family-oriented all-inclusive resort in Boğazkent (Belek/Serik area of Antalya), operated by Crystal Hotels (Kilit Hospitality Group). It features multiple waterparks/slides, several dining venues, a private beach, and a dedicated children's program called Crispy Kids World.",
            amenitySlugs: ["wifi", "air-conditioning", "pool", "beach-access", "family-friendly", "breakfast"],
            rooms: [("Standard Room", 2, 3800m), ("Family Room", 4, 5400m), ("Suite", 4, 7600m)],
            images:
            [
                "https://static.kilithg.net/networks/2/properties/20/multimedia/20240227082017386_org.jpg",
                "https://static.kilithg.net/networks/2/properties/20/multimedia/20240227082017348_org.jpg",
                "https://static.kilithg.net/networks/2/properties/20/multimedia/20240227082017372_org.jpg",
                "https://static.kilithg.net/networks/2/properties/20/multimedia/20240308123215503_org.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Delphin Imperial", slug: "delphin-imperial",
            website: "https://www.delphinhotel.com/en/delphin-imperial",
            city: "Antalya", address: "Kemerağzı Mahallesi, Yaşar Sobutay Bulvarı No:344, Aksu",
            lat: 36.838, lon: 30.817, star: null,
            googleRating: 4.6m, googleRatingCount: 7340,
            phone: "+90 242 320 07 07",
            description: "A large all-inclusive beachfront resort near Lara/Aksu, Antalya, part of the Delphin Hotels & Resorts group. It consists of one nine-floor main building with 798 rooms, 11 à la carte restaurants, a 1,560 m² multi-purpose hall, a bowling alley, a cinema, and spa facilities.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "spa", "bar", "breakfast"],
            rooms: [("Standard Room", 2, 4800m), ("Deluxe Room", 3, 6800m), ("Suite", 4, 9800m)],
            images:
            [
                "https://www.delphinhotel.com/main_pics/pages/medium/2996.png",
                "https://www.delphinhotel.com/main_pics/pages/medium/709.png",
                "https://www.delphinhotel.com/main_pics/pages/medium/3001.png",
            ]));

        hotels.Add(Build(amenities,
            name: "Sueno Hotels Deluxe Belek", slug: "sueno-hotels-deluxe-belek",
            website: "https://deluxe.sueno.com.tr/en/",
            city: "Antalya", address: "Akkınlar Mahallesi, Taşlıburun Cad. No:4, Kadriye-Serik (Belek)",
            lat: 36.877, lon: 31.036, star: null,
            googleRating: 4.4m, googleRatingCount: 5649,
            phone: "+90 242 710 30 00",
            description: "A beachfront resort in the Kadriye district of Belek, Antalya, near several golf courses, with multiple restaurants and bars, a spa/wellness center, and extensive sports and entertainment facilities. Marketed as combining modern design with traditional Turkish hospitality.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "spa", "bar", "breakfast"],
            rooms: [("Standard Room", 2, 4600m), ("Deluxe Room", 3, 6400m), ("Suite", 4, 9000m)],
            images:
            [
                "https://deluxe.sueno.com.tr/wp-content/uploads/2024/05/sueno-hotels-deluxe-belek-antalya-sueno-square-highlighted-photo-2.webp",
                "https://deluxe.sueno.com.tr/wp-content/uploads/2024/04/Sueno-hotels-deluxe-belek-italian-a-la-carte-restaurant.webp",
                "https://deluxe.sueno.com.tr/wp-content/uploads/2024/04/Sueno-hotels-deluxe-belek-spa-center.webp",
                "https://deluxe.sueno.com.tr/wp-content/uploads/2024/04/sueno-hotels-deluxe-belek-wellness.webp",
            ]));

        hotels.Add(Build(amenities,
            name: "Ramada Plaza by Wyndham Antalya", slug: "ramada-plaza-antalya",
            website: "https://ramadaplazaantalya.com/En",
            city: "Antalya", address: "Gençlik Mah., Fevzi Çakmak Cd. No:22, Muratpaşa",
            lat: 36.888, lon: 30.704, star: null,
            googleRating: 4.2m, googleRatingCount: 6825,
            phone: "+90 242 249 11 11",
            description: "A hotel on Fevzi Çakmak Caddesi in central Antalya, near the Mediterranean coast and the city's historical sites. It offers rooms with Mediterranean views, multiple dining venues, a 2,700 m² spa and fitness center, and indoor/outdoor pools.",
            amenitySlugs: ["wifi", "air-conditioning", "gym", "spa", "pool", "breakfast", "parking"],
            rooms: [("Standard Room", 2, 3200m), ("Deluxe Room", 2, 4200m), ("Suite", 3, 6000m)],
            images:
            [
                // Reordered 2026-08-12: added this infinity-pool/sea-view shot (Ramada branding
                // visible on the railing) from the hotel's own "Beach & Pools" gallery as lead —
                // the previous 3 images were all room/bathroom interiors, no exterior at all.
                "https://ramadaplazaantalya.com/assets/images/gallery/22228940-ab7b-4233-90d5-b8df5d728290.jpg",
                "https://ramadaplazaantalya.com/assets/images/gallery/2c4a6a34-b105-4537-82f6-51c9e5412f8d.jpg",
                "https://ramadaplazaantalya.com/assets/images/gallery/cc207ee5-eb96-4bef-9809-95f292244ec2.jpg",
                "https://ramadaplazaantalya.com/assets/images/gallery/26e7ddbc-3996-4e20-90eb-f7fad4859945.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Sherwood Exclusive Kemer", slug: "sherwood-exclusive-kemer",
            website: "https://sherwoodhotels.com.tr/en/our-hotels/sherwood-exclusive-kemer/",
            city: "Antalya", address: "Cumhuriyet Mahallesi, Ahu Ünal Aysal Caddesi No:37, Göynük (Kemer)",
            lat: 36.575, lon: 30.545, star: null,
            googleRating: 4.5m, googleRatingCount: 5050,
            phone: "444 9 051",
            description: "A resort in the Göynük area of Kemer, Antalya, set between the Beydağları Mountains and the Mediterranean coast amid pine forest. Offers accommodation from standard rooms to villas, multiple dining venues, spa facilities, an aquapark, and a private beach with two piers.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "pool", "spa", "family-friendly", "breakfast"],
            rooms: [("Standard Room", 2, 4000m), ("Deluxe Room", 3, 5600m), ("Villa", 5, 12000m)],
            images:
            [
                // Reordered 2026-08-12: the hotel's own homepage "our hotels" section uses this
                // aerial mountain/beach/resort shot as Sherwood Exclusive Kemer's own cover photo —
                // moved to lead so the homepage card shows the property, not a room interior.
                "https://sherwoodhotels.com.tr/media/ji3p15o1/sherwood-ana-sayfa-otellerimiz-section-sherwood-exculisive-kemer-card-desktop.jpg",
                "https://sherwoodhotels.com.tr/media/o5yg2egk/sherwood-exclusive-kemer-standar-oda-list-card.jpg",
                "https://sherwoodhotels.com.tr/media/ofldbacm/sherwood-exclusive-kemer-deluxe-oda-list-card.jpg",
                "https://sherwoodhotels.com.tr/media/jtnlv5ub/sherwood-exclusive-kemer-lagoon-oda-list-card.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Selectum Family Resort Belek", slug: "selectum-family-resort-belek",
            website: "https://familyresortbelek.selectumhotels.com",
            city: "Antalya", address: "Kongre Caddesi No: 364, İleribaşı Mevkii (Belek)",
            lat: 36.862, lon: 31.033, star: null,
            googleRating: 4.8m, googleRatingCount: 8851,
            phone: "+90 242 710 33 00",
            description: "A family-oriented all-inclusive resort in Belek where the Acısu River meets the Mediterranean, surrounded by roughly 75 acres of forest. The property has 349 rooms with balconies and includes an aquapark, multiple pools, direct beach access, and a kids' club (\"Puppies' World\").",
            amenitySlugs: ["wifi", "air-conditioning", "pool", "beach-access", "family-friendly", "breakfast"],
            rooms: [("Standard Room", 2, 3600m), ("Family Room", 4, 5200m), ("Suite", 4, 7400m)],
            images:
            [
                // Reordered 2026-08-12: was leading with a bedroom photo; this beach-lounge shot
                // actually shows the property (loungers, umbrellas, palms) instead of a generic room.
                "https://content.anexapps.com/v1/selectum-family-resort-belek/026f961b683144c6a587baf67f139614.jpg",
                "https://content.anexapps.com/v1/selectum-family-resort-belek/005c082fe5c0436090c5f1a34c521fd2.jpg",
                "https://content.anexapps.com/v1/selectum-family-resort-belek/00f92e87d5b445998a821d2c8a13c721.jpg",
                "https://content.anexapps.com/v1/selectum-family-resort-belek/03bacda21fdb42a6af446caf7224a405.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Papillon Ayscha", slug: "papillon-ayscha",
            website: "https://papillon.com.tr/en/",
            city: "Antalya", address: "Belek Mahallesi, İleribaşı Mevkii, Belek Turizm Merkezi, Serik",
            lat: 36.8628, lon: 31.0550, star: null,
            googleRating: 4.6m, googleRatingCount: 3560,
            phone: "+90 242 710 12 00",
            description: "A full-service all-inclusive resort in the Belek Tourism Center offering room categories from Standard through Presidential Villa. Features REBORN SPA & wellness facilities with a Turkish bath, a Fit Club fitness area, a kids' program (\"Papy Kids World\"), and multiple à la carte restaurants.",
            amenitySlugs: ["wifi", "air-conditioning", "spa", "gym", "family-friendly", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 4200m), ("Deluxe Room", 3, 5800m), ("Presidential Villa", 6, 15000m)],
            images:
            [
                // Replaced 2026-08-12: the previous 4 images were all staged lifestyle/ad photography
                // (a couple posing, a family in bed) with no real shot of the property. These are
                // genuine drone photos from the hotel's own site showing the actual resort.
                "https://papillon.com.tr/wp-content/uploads/2026/01/Papillon-Ayscha-Inspire-Drone-10-1536x1023.jpg",
                "https://papillon.com.tr/wp-content/uploads/2026/01/Papillon-Ayscha-Inspire-Drone-13-1536x1023.jpg",
                "https://papillon.com.tr/wp-content/uploads/2026/01/Papillon-Ayscha-Inspire-Drone-17-1536x1023.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Robinson Nobilis", slug: "robinson-nobilis",
            website: "https://www.robinson.com/en/en/resort-holiday/turkey/nobilis/club-details/",
            city: "Antalya", address: "Acısu Mevkii, P.O. Box 56-Serik (Belek)",
            lat: 36.85, lon: 31.11, star: null,
            googleRating: 4.7m, googleRatingCount: 2178,
            phone: "+90 242 322 71 00",
            description: "An all-inclusive resort on Turkey's southern (Belek) coast, set on a river with the resort's grounds separated from a 20 km sandy beach reached via a bridge. It spans roughly 850,000 m², has 404 rooms, and offers golf, tennis, and wellness programs, positioned as \"adults at the center, children welcome.\"",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "spa", "family-friendly", "breakfast"],
            rooms: [("Standard Room", 2, 4400m), ("Deluxe Room", 3, 6000m), ("Suite", 4, 8600m)],
            images:
            [
                "https://www.robinson.com/media/_processed_/3/c/csm_RCNL_Beach_14910_WOL_5e98daa86c.jpg",
                "https://www.robinson.com/media/_processed_/2/9/csm_RCSN_Drone_Resort_15829_WOL_c824548e90.jpg",
                "https://www.robinson.com/media/_processed_/2/d/csm_RCNL_Golf_16492_WOL_2cd5093cf0.jpg",
                "https://www.robinson.com/media/_processed_/0/0/csm_nl_14386_Indoor_pool_WOL_15ad0f1a58.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "AKKA Hotels Alinda", slug: "akka-hotels-alinda",
            website: "https://www.akkahotels.com/alinda/en",
            city: "Antalya", address: "Kiriş Mahallesi, Sahil Caddesi 12. Sokak No.3, Kemer",
            lat: 36.575, lon: 30.5806, star: 5,
            googleRating: 4.7m, googleRatingCount: 6079,
            phone: "+90 242 824 71 30",
            description: "A 5-star seafront family hotel in the Kiriş district of Kemer, named after an ancient Anatolian city and located near the historical Olympos and Phaselis ancient sites. Offers varied room categories and multiple dining venues catering to families and different age groups.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "family-friendly", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 4200m), ("Deluxe Room", 3, 5800m), ("Suite", 4, 8200m)],
            images:
            [
                "https://cdn.akkahotels.com/Uploads/Cms/akka-alinda-otel-havuz-bar-2_2.jpeg",
                "https://cdn.akkahotels.com/Uploads/Cms/akka-alinda-otel-plasj-ve-havuz-6_1.jpeg",
                "https://cdn.akkahotels.com/Uploads/Cms/akka-alinda-otel-lobby-bar-1_7.jpg",
                "https://cdn.akkahotels.com/Uploads/Cms/alinda-king-suite.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "AKKA Hotels Claros", slug: "akka-hotels-claros",
            website: "https://www.akkahotels.com/claros/en",
            city: "Antalya", address: "Kiriş Mahallesi, Sahil Caddesi 12. Sokak, Kemer",
            lat: 36.576, lon: 30.5810, star: 4,
            googleRating: 4.5m, googleRatingCount: null,
            phone: "+90 242 824 71 45",
            description: "A 4-star family hotel in the Kiriş district of Kemer, located in the heart of nature surrounded by orange trees and pine woods. Designed to provide a comfortable, all-inclusive holiday especially for families with children, with an outdoor pool and spa facilities.",
            amenitySlugs: ["wifi", "air-conditioning", "pool", "spa", "parking", "family-friendly", "breakfast"],
            rooms: [("Standard Room", 2, 3400m), ("Duplex Room", 3, 4600m), ("Family Room", 4, 5400m)],
            images:
            [
                // Reordered 2026-08-12: added this aerial shot from the hotel's own gallery as lead —
                // was a restaurant interior photo, which doesn't show the property itself.
                "https://cdn.akkahotels.com/Uploads/Gallery/x3_55-min.webp",
                "https://cdn.akkahotels.com/Uploads/Cms/claros-otel-ana-restoran-12_1.jpg",
                "https://cdn.akkahotels.com/Uploads/Gallery/akka-hotels-claros-dubleks-oda-2.jpg",
                "https://cdn.akkahotels.com/Uploads/Cms/lobby-bar-claros-otel_2.jpeg",
            ]));

        hotels.Add(Build(amenities,
            name: "Nirvana Mediterranean Excellence", slug: "nirvana-mediterranean-excellence",
            website: "https://www.nirvanahotels.com.tr/en/hotels/nirvana-mediterranean-excellence",
            city: "Antalya", address: "Göynük Mahallesi, Başkomutan Atatürk Caddesi No: 141, Beldibi (Kemer)",
            lat: 36.7275, lon: 30.5539, star: null,
            googleRating: 4.7m, googleRatingCount: 4158,
            phone: "444 0 644",
            description: "A resort in Beldibi, Kemer, set between the Mediterranean Sea and the Toros Mountains, with cedar-house style accommodations within a red pine forest setting. The property has three piers/pavilions and offers a serene holiday experience amid the natural surroundings of Beldibi.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 4300m), ("Deluxe Room", 3, 5900m), ("Suite", 4, 8400m)],
            images:
            [
                // Replaced 2026-08-12: the hotel's homepage "multimedia" carousel returned generic
                // branded lifestyle/service ads (dessert, spa, gym, staff, a boutique storefront) —
                // none showed the actual property. These come from the hotel's own dedicated
                // /gallery page instead, which has real resort photos.
                "https://static.kilithg.net/networks/1/properties/1/galleries/1/20240812124711159_org.jpg",
                "https://static.kilithg.net/networks/1/properties/1/galleries/1/20240812124707898_org.JPG",
                "https://static.kilithg.net/networks/1/properties/1/galleries/1/20240812124713359_org.jpg",
                "https://static.kilithg.net/networks/1/properties/1/galleries/1/20240812124708908_org.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Orange County Kemer", slug: "orange-county-kemer",
            website: "https://www.orangecounty.com.tr/kemer/en",
            city: "Antalya", address: "Atatürk Bulvarı, Yeni Mahalle, Kemer",
            lat: 36.6019, lon: 30.5592, star: null,
            googleRating: 4.6m, googleRatingCount: 11884,
            phone: "444 2000",
            description: "An all-inclusive resort in central Kemer designed around a Dutch/Amsterdam architectural theme, with views of the Taurus Mountains. Has a private 90-meter pebble beach, room types from Standard to the \"Queen Beatrix\" suite category, a Turkish bath/sauna/fitness center, and a 600 m² event hall.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "pool", "gym", "spa", "breakfast"],
            rooms: [("Standard Room", 2, 3400m), ("Deluxe Room", 3, 4800m), ("Queen Beatrix Suite", 4, 7200m)],
            images:
            [
                // Reordered 2026-08-12: dropped "Image-271.webp", which turned out to be a DJ-night
                // event flyer (a person's face, an event date, a phone number) rather than a hotel
                // photo, despite the generic filename. These three are genuine property photos.
                "https://www.orangecounty.com.tr/photos/orange-county-OTELIMIZ-12210261.webp",
                "https://www.orangecounty.com.tr/photos/orange-county-HAVUZ___PLAJ-5817141.webp",
                "https://www.orangecounty.com.tr/photos/orange-county-ODALAR-284821.webp",
            ]));

        hotels.Add(Build(amenities,
            name: "Martı Resort", slug: "marti-resort",
            website: "https://www.marti.com.tr/marti-resort-marmaris",
            city: "Marmaris", address: "İçmeler, Marmaris",
            lat: 36.8028, lon: 28.2342, star: null,
            googleRating: 4.2m, googleRatingCount: 3179,
            phone: "+90 252 455 34 40",
            description: "A beachfront property in İçmeler, Marmaris, operated by Martı Hotels & Marinas, a group with over 50 years in the region. Offers multiple dining venues, wellness facilities, and activities including water sports along the coastline, positioned as family- and couple-friendly while also welcoming pets.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "pet-friendly", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 3000m), ("Deluxe Room", 3, 4200m), ("Suite", 4, 6000m)],
            images:
            [
                "https://irp.cdn-website.com/2e136757/dms3rep/multi/opt/DSC00230-1920w.jpg",
                "https://irp.cdn-website.com/2e136757/dms3rep/multi/opt/DJI_0797-1920w.jpg",
                "https://irp.cdn-website.com/2e136757/dms3rep/multi/opt/Resort-General-10-1920w.jpg",
                "https://irp.cdn-website.com/2e136757/dms3rep/multi/opt/resort-the-adult-restaurant-7-1920w.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Lykia World Antalya", slug: "lykia-world-antalya",
            website: "https://lykiaworld.com/en/hotels/lykia-world-antalya",
            city: "Antalya", address: "Kundu Mahallesi, Yaşar Sobutay Bulvarı No:100, Aksu",
            lat: 36.8742, lon: 30.9108, star: null,
            googleRating: 4.3m, googleRatingCount: 7720,
            phone: "+90 242 277 13 34",
            description: "A beachfront resort in the Kundu tourism zone of Aksu, Antalya, featuring a 2.5 km stretch of coastline and Turkey's first Links-style golf course. The grounds sit between the Mediterranean shoreline and the Taurus foothills, with five on-site restaurants, spa facilities, and sports amenities.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "spa", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 4000m), ("Deluxe Room", 3, 5600m), ("Suite", 4, 8000m)],
            images:
            [
                "https://offload-assets.framercoder.com/66454b3a-4086-47fd-bfa4-4dbfa7265cd1/3f5973b5-56ab-48bb-8851-132a4b075749_JSIc1OKtGmN6S9GXmjzZQgQvU.jpg",
                "https://offload-assets.framercoder.com/66454b3a-4086-47fd-bfa4-4dbfa7265cd1/aa947b87-d3db-44c3-891a-082dc008eb48_MPlDCcOtX6TamTtfHerfNc2Q.jpg",
                "https://offload-assets.framercoder.com/66454b3a-4086-47fd-bfa4-4dbfa7265cd1/ca35ba44-eb46-41c3-ab45-6de304a1b568_iiEdIBsKa7mGra8Vs2S3ReJyxo.jpg",
                "https://offload-assets.framercoder.com/66454b3a-4086-47fd-bfa4-4dbfa7265cd1/d5cb1e75-430e-4ea1-9879-be53102f5b48_n21l0TAVVXii7DslI9Qs65um1s.jpg",
            ]));

        hotels.Add(Build(amenities,
            name: "Dalyan Michelangelo Boutique Hotel", slug: "dalyan-michelangelo-boutique-hotel",
            website: "https://www.michelangelo.com.tr/en/",
            city: "Dalyan", address: "Dalyan Mah. Çınar Sok. No:3, Ortaca",
            lat: 36.8347, lon: 28.6432, star: null,
            googleRating: 4.7m, googleRatingCount: 336,
            phone: "+90 252 281 14 14",
            description: "A 53-room boutique hotel in Dalyan, on Turkey's Aegean coast, offering dining that blends Turkish and Ottoman cuisine. Includes a swimming pool, fitness center, and spa services, positioned as a smaller, personalized alternative to large resort hotels.",
            amenitySlugs: ["wifi", "air-conditioning", "pool", "gym", "spa", "breakfast"],
            rooms: [("Standard Room", 2, 2200m), ("Suite", 3, 3400m)],
            images:
            [
                "https://www.michelangelo.com.tr/wp-content/uploads/2024/11/IMG_5211-scaled.jpeg",
                "https://www.michelangelo.com.tr/wp-content/uploads/2024/11/IMG_5145-scaled.jpeg",
                "https://www.michelangelo.com.tr/wp-content/uploads/2024/11/IMG_5205-scaled.jpeg",
                "https://www.michelangelo.com.tr/wp-content/uploads/2024/11/IMG_5158-scaled.jpeg",
            ]));

        hotels.Add(Build(amenities,
            name: "Trendy Lara", slug: "trendy-lara",
            website: "https://trendy.com.tr/en/trendy-lara/",
            city: "Antalya", address: "Kundu Mh. Yaşar Sobutay Blv. No:104, Aksu",
            lat: 36.8742, lon: 30.9108, star: null,
            googleRating: 4.6m, googleRatingCount: 5971,
            phone: "+90 242 321 25 10",
            description: "A beachfront hotel next to Lara Beach in Aksu, Antalya, built in 2016, with rooms ranging from 36 to 120 square meters including garden suites and villas. Includes five à la carte restaurants, a private sandy beach, a spa and fitness center, an aquapark, and sports facilities.",
            amenitySlugs: ["wifi", "air-conditioning", "beach-access", "pool", "spa", "gym", "breakfast", "bar"],
            rooms: [("Standard Room", 2, 4100m), ("Garden Suite", 3, 5700m), ("Villa", 5, 11000m)],
            images:
            [
                // Reordered 2026-08-12: added this aerial aquapark shot (hotel's own branding visible
                // on the structure) as lead — was leading with a standard room photo.
                "https://trendy.com.tr/wp-content/uploads/2023/09/Trendy-Lara-Hava-04-Orta_Boyut.webp",
                "https://trendy.com.tr/wp-content/uploads/2023/03/trendy-lara-standard-room-6.webp",
                "https://trendy.com.tr/wp-content/uploads/2023/04/trendy-lara-garden-suite-2.webp",
                "https://trendy.com.tr/wp-content/uploads/2023/04/trendy-lara-duplex-room-1.webp",
            ]));

        hotels.Add(Build(amenities,
            name: "Miarosa Kemer Beach", slug: "miarosa-kemer-beach",
            website: "https://miarosakemerbeach.com/",
            city: "Antalya", address: "Kiriş, Sahil Cd. No:48, Kemer",
            lat: 36.5750, lon: 30.5806, star: null,
            googleRating: 4.1m, googleRatingCount: 3761,
            phone: "+90 242 824 61 07",
            description: "An all-inclusive hotel in the Kiriş neighborhood of Kemer, located roughly 500-600 meters from the sea with its own sand beach. Offers indoor and outdoor pools, multiple restaurants, and spa/massage facilities, with family-oriented services such as baby care.",
            amenitySlugs: ["wifi", "air-conditioning", "pool", "beach-access", "spa", "family-friendly", "breakfast"],
            rooms: [("Standard Room", 2, 2800m), ("Family Room", 4, 4000m)],
            images:
            [
                // Reordered 2026-08-12: images 1-2 are an artsy underwater shot of sharks/rays near
                // their pier — a real photo from their site, but it doesn't show the hotel itself,
                // so it shouldn't be the lead/thumbnail image. These two (pool with statue, kids'
                // water playground) clearly show the actual property.
                "https://miarosakemerbeach.com/images/gallery/3.webp",
                "https://miarosakemerbeach.com/images/gallery/4.webp",
                "https://miarosakemerbeach.com/images/gallery/1.webp",
                "https://miarosakemerbeach.com/images/gallery/2.webp",
            ]));

        hotels.Add(Build(amenities,
            name: "Ares Blue Hotel", slug: "ares-blue-hotel",
            website: "https://aresbluehotels.com/en/",
            city: "Antalya", address: "Kiriş Sahil Caddesi No:1, Kemer",
            lat: 36.5750, lon: 30.5806, star: null,
            googleRating: 2.8m, googleRatingCount: 615,
            phone: "+90 242 606 12 80",
            description: "A garden hotel in the Kiriş area of central Kemer, built on roughly 4.5 acres among flowers and orange trees. This all-inclusive hotel has 150 rooms across three blocks, sits about 300 meters from the beach, and offers on-site dining and pool facilities.",
            amenitySlugs: ["wifi", "air-conditioning", "pool", "beach-access", "breakfast"],
            rooms: [("Standard Room", 2, 2600m), ("Deluxe Room", 3, 3600m)],
            images:
            [
                "https://aresbluehotels.com/img/blue4.webp",
                "https://aresbluehotels.com/img/blue2.webp",
                "https://aresbluehotels.com/img/blue3.webp",
                "https://aresbluehotels.com/img/oda1.webp",
            ]));

        return hotels;
    }

    private static Hotel Build(
        Dictionary<string, Amenity> amenities,
        string name,
        string slug,
        string website,
        string city,
        string address,
        double lat,
        double lon,
        int? star,
        decimal? googleRating,
        int? googleRatingCount,
        string? phone,
        string description,
        string[] amenitySlugs,
        (string Name, int Capacity, decimal Price)[] rooms,
        string[] images)
    {
        var hotel = new Hotel
        {
            Name = name,
            Slug = slug,
            Description = description,
            City = city,
            Country = "Turkey",
            Address = address,
            Latitude = lat,
            Longitude = lon,
            StarRating = star,
            OfficialWebsiteUrl = website,
            PhoneNumber = phone,
            GoogleRating = googleRating,
            GoogleRatingCount = googleRatingCount,
            GoogleRatingCapturedAtUtc = googleRating.HasValue ? GoogleRatingCapturedAt : null,
        };

        hotel.Images = images
            .Select((url, index) => new HotelImage
            {
                HotelId = hotel.Id,
                Url = url,
                AltText = $"{name} — photo {index + 1}",
                DisplayOrder = index + 1,
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
                FullName = "Reservations Team",
                Email = $"reservations@{slug}.example",
            }
        ];

        hotel.HotelAmenities = amenitySlugs
            .Select(s => new HotelAmenity { HotelId = hotel.Id, AmenityId = amenities[s].Id })
            .ToList();

        return hotel;
    }
}
