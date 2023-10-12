using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;


namespace Symulation
{
    class Program
    {
        static void Main(string[] args)
        {
                //System.Console.WriteLine(test_method());
            
                
                // chce zmienic predkosci wiec pytanie
                // czy to w modulach czy gdzies dalej 
                var City = new LibraryOfCities();
                var (CityMap,list_of_modules) = City.MiastoModulowe_v_1();
                
                CityMap.CalculateDirectionalityOfTracksInCity();
                
                CityMap.CalculateSpeedLimitForTracks();
                
                //calculate position of nodes 
                CityMap.CalculatePositionOfNodes(true);

                var routeSearch = new SearchForRoutesMain(CityMap);

                //var route = routeSearch.SeekRouteBetweenStations (1, 2);
                //var profileGenerator = new FastestProfile(route[0], CityMap);
                
                // jesli chce zrobic wykres tego jaka jast wydajnosc w funkcji dlugasci sort
                // jedna petla bedzie cisnac po dlugosci sort
                // druga po przesunieciu synchronizacji DC
                // wiec bede miec macierz wynikow i z nich moge zrobic wykres

                // moge ewentualnie zrobic tak ze bedzie sie kilka razy liczyc na jednym ustawieniu
                // zeby bylo tak ze zmniejsze wplyw losowosci/szum dzieki temu 
                // wtedy wykresy beda ladniej wygladac co jest zajebiaszcze 

                 //var sym_control = new Symulation_control(CityMap, list_of_modules, 24, 0, 12, 1.45);
                 //var ride_repository = sym_control.run_symulation_v1(12000, 12350, 0.95);
                // petla zewnetrzna - dlugosc sekcji sort
                    // pertla  - shift sekcji DC
                        // petla z kilkukrotnym przeliczeniem x10 brzmi dobrze  - eliminacja szumu  
                        // zapisuje sredni wynik w macierzy 


                
                double sort_shift = 0;
                var results = new Sym_result_storage("wyniki.txt");
                var list_of_DC_sizes = new List<double>();
                var list_of_DC_shifts = new List<double>();
                // tu robie przygotowanie pliku tekstowego
                for (double DC_size = 4; DC_size < 15; DC_size += 0.25)
                {
                    list_of_DC_sizes.Add(DC_size);
                    
                    for(double DC_shift = 0; DC_shift <= 3; DC_shift += 0.1)
                    {
                        if(!list_of_DC_shifts.Contains(DC_shift))
                            list_of_DC_shifts.Add(DC_shift);

                        var sum_of_rides = 0;
                        for(int i=0; i < 2; i++)
                        {
                            var sym_control = new Symulation_control(CityMap, list_of_modules, DC_size * 2, sort_shift, DC_size, DC_shift);
                            var ride_repository = sym_control.run_symulation_v1(12000, 12200, 0.9);
                            sum_of_rides += ride_repository.return_number_of_rides();
                        }
                        var average = sum_of_rides / 2;
                        
                        results.add_data_to_storage(DC_size*2,sort_shift,DC_size,DC_shift,average);
                    }
                }
                

                // brakuje mi tego zeby zrobic tutaj header do danych 
                // wogule jest tak ze przechowywanie danych bez headera to jest jakis rzarcik bo to ma mega nieciekawe 
                // konsekwencje. szalone odczytywanie jakie tam sa w srodku dane itp. nigdzy nie wiadomo czy zrobie to ok czy nie i takie tam

                // to co zrobie to:
                // DC_size list, DC_shift list
                // narazie to powinno wystarczyc
                results.create_file_header(list_of_DC_sizes,list_of_DC_shifts);
                results.save_stored_data();

                //System.Console.WriteLine("test");
                /*
                var pod_animation = new OfflineRideDataPreparation(ride_repository);
                var track_disp = new TrackDisplayDataPreparation(sym_control);

                pod_animation.apply_all_mods_in_symulation_rides();
                Console.WriteLine("mods update complete");

                pod_animation.update_profile_for_all_rides();
                Console.WriteLine("profile update ready");

                //pod_animation.sort_sections_time_travel_check();
                //pod_animation.DC_section_time_travel_check();

                pod_animation.calculate_position_data_and_activation(0.025d);
                Console.WriteLine("calculation of position done");

                pod_animation.save_position_data_to_file();
                Console.WriteLine("preparation position data file ,  ready");

                track_disp.prepare_data_for_track_display();
                Console.WriteLine("track position data prepared");
                
                track_disp.save_data_to_file();
                */
                //pod_animation.statistics_of_directions();


            /*
            for (double i = 30; i < 45; i++)   //sort length
            {
                for (double j = 10; j < 14; j++)   // DC length
                {
                
                var symulation = new Symulation_control(CityMap,list_of_modules, i, 0, j);// w tej mapie sort powinien miec max 28 m inaczej cos jest nietak z mapa
                
                var czas_startu = DateTime.Now;
                var ride_repositiory = symulation.run_symulation_v1(12000, 13200, 0.4, predkosc);
                var czas_konca = DateTime.Now;
                var czas_obliczen = czas_konca - czas_startu;
                
                //Console.WriteLine("czas obliczen {0}", czas_obliczen.Seconds);
                var pod_animation = new OfflineRideDataPreparation(ride_repositiory);
                var track_disp = new TrackDisplayDataPreparation(symulation);

                //pod_animation.apply_all_mods_in_symulation_rides();
                //Console.WriteLine("mods update complete");

                //pod_animation.update_profile_for_all_rides();
                //Console.WriteLine("profile update ready");

                //pod_animation.sort_sections_time_travel_check();
                //pod_animation.DC_section_time_travel_check();

                //pod_animation.calculate_position_data_and_activation(0.025d);
                //Console.WriteLine("calculation of position done");

                //pod_animation.save_position_data_to_file();
                //Console.WriteLine("preparation position data file ,  ready");

                //track_disp.prepare_data_for_track_display();
                //Console.WriteLine("track position data prepared");
                
                //track_disp.save_data_to_file();

                //pod_animation.statistics_of_directions();

                }
            }
            */

            Console.WriteLine("ready");
            Console.WriteLine("radosc :)");
            
        }

        
        
        public static double get_speed_at_distance_from_profile(List<double[]> profile, double distance_from_start)
        {
            double speed = 0;
            double distance_into_affected_track = 0;
            double distance_from_affected_end_of_track = 0;
            double initial_speed;
            double end_speed;
            double acceleration;
            int index_of_profile = 0;

            //znajduje w ktorym odcinku profilu jest punkt docelowy
            for(int i=0; i<profile.Count; i++)
            {
                if(profile[i][0] <= distance_from_start && profile[i][1] > distance_from_start)
                {
                    index_of_profile = i;
                    //licze na ile wchodzi dany punkt w odcinek docelowy
                    distance_into_affected_track = distance_from_start - profile[i][0];
                    distance_from_affected_end_of_track = profile[i][1] - distance_from_start;
                    break;
                }
            }

            //sprawdzam w jakim rodzaju odcinka jest punkt docelowy 
            switch (profile[index_of_profile][4])
            {
                //jesli jest to 0 (track) to podaje poprostu predkos danego tracku
                case 0:
                    
                    speed = profile[index_of_profile][2];

                    break;
                //jesli jest to 1 (acceleration) to licze predkosc z wzoru na bazie odleglosci od poczatku przyspieszania
                case 1:

                    initial_speed = profile[index_of_profile][2];
                    acceleration = profile[index_of_profile][3];
                    speed = (-initial_speed + Math.Sqrt(Math.Pow(initial_speed,2) + 2 * distance_into_affected_track * acceleration)) + initial_speed;

                    break;

                //jesli jest to -1 (brake) to licze predkosc z wzoru na bazie odleglosci (prawdopodobnie od konca tak jak byl liczony)
                case -1:
                    end_speed = profile[index_of_profile][2];
                    acceleration = profile[index_of_profile][3];

                    speed = (-end_speed + Math.Sqrt(Math.Pow(end_speed, 2) + 2 * distance_from_affected_end_of_track * acceleration)) + end_speed;

                    break;
            }
            return speed;

        }

        public static double get_time_at_distance_from_profile(List<double[]> profile, double distance_from_start)
        {
            double speed = 0;
            double distance_into_affected_track = 0;
            double distance_from_affected_end_of_track = 0;
            double initial_speed;
            double initial_time;
            double time_at_end_of_affected_track;
            double end_speed;
            double acceleration;
            int index_of_profile = 0;

            //znajduje w ktorym odcinku profilu jest punkt docelowy
            for (int i = 0; i < profile.Count; i++)
            {
                if (profile[i][0] <= distance_from_start && profile[i][1] > distance_from_start)
                {
                    index_of_profile = i;
                    //licze na ile wchodzi dany punkt w odcinek docelowy
                    distance_into_affected_track = distance_from_start - profile[i][0];
                    distance_from_affected_end_of_track = profile[i][1] - distance_from_start;
                    break;
                }
            }

            //sprawdzam w jakim rodzaju odcinka jest punkt docelowy 
            switch (profile[index_of_profile][4])
            {
                //jesli jest to 0 (track) to podaje poprostu predkos danego tracku
                case 0:

                    speed = profile[index_of_profile][2];

                    break;
                //jesli jest to 1 (acceleration) to licze predkosc z wzoru na bazie odleglosci od poczatku przyspieszania
                case 1:

                    initial_speed = profile[index_of_profile][2];
                    initial_time = profile[index_of_profile][7];
                    acceleration = profile[index_of_profile][3];
                    speed = (-initial_speed + Math.Sqrt(Math.Pow(initial_speed, 2) + 2 * distance_into_affected_track * acceleration))/acceleration + initial_time; 

                    break;

                //jesli jest to -1 (brake) to licze predkosc z wzoru na bazie odleglosci (prawdopodobnie od konca tak jak byl liczony)
                case -1:
                    end_speed = profile[index_of_profile][2];
                    time_at_end_of_affected_track = profile[index_of_profile][8];
                    acceleration = profile[index_of_profile][3];

                    speed = -(-end_speed + Math.Sqrt(Math.Pow(end_speed, 2) + 2 * distance_from_affected_end_of_track * acceleration))/acceleration + time_at_end_of_affected_track;

                    break;
            }
            return speed;

        }




        // [] w przygotowaniu danych pozycji dla sort sekcji brakuje mi dodania przsuniecie na bok
        // to powinienem zrobic zeby to dobrze wygladalo, jest to dosc latwe.
        
        // dziele dlugosc permutacji sort na 3 odcinki
        // zmiana pasa, czesc zmiany pozycji, zmiana pasa
        
        // jeszcze musze jakos odroznic pomiadzy soba ride ktore jada roznymi pasami
        // zrobie to po oknach exit traversu
        
        
        // napisze nowa metode, bedzie zwracac numer aktywnego okna na wyjsciu z exit
            
        
        // dla danego ride sprawdzam numer okna na wyjsciu
            //prawy lub lewy pas
        // licze w jakiej jest sekcji
            //licze boczne przesuniecie dla danej pozycji
            //robie translacje pozycji


        // [] w pozycjonowaniu kapsol jest blad, bo sa umieszczane od samego poczatku traversu. a powinien byc jakis dystans zeby niewystawaly do przodu
        // narazie to niema wiekszego znaczenia, ale w praktyce zmiana jest banalna 
        // jest metoda ktra podaje pozycje w oknie na podstawie indexu, wystarczy to zmodyfikowac



        // to jest ostatnia zecz w prostym ukladzie 

        // [] w definicji miasta brakuje mi jeszcze uaktywnienia zaokraglonych odcinkow, trackow
        // wymaga to doatkowago, napisania kilku metod, jak liczenie pozycji, generowanie nowej geometrii przy modach itp (tutaj to chyba wystarczy add new node method) 
                //to jest mega detal i narazie tego nieruszam, duza ilosc pracy w stosunku do zysku



        //kolejny etap zaawansowania

        // add algorith for ride search. with place for search for pod and place at the end
        // base for that will be station object with load list for each pod slot
                //to jest juz zaawansowane, i wymaga wejscia na kolejny poziom abstrakcji
                //zrobie to dopiero jak podstawa bedzie dzialac

        // nowe rodzaje skrzyzowan
                // koniczynka
                // skzyzowanie z D bez sort

        // zbudowanie konfiguracji strowanej z zewnatrz tak zeby moc szukac najbardziej optymalnych ustawien
        // przez uczenie maszynowe
    }

    public class Sym_result_storage
    {
        string data_storage_file;
        StringBuilder builder;
        List<string> list_of_header_lines;
        List<string> list_of_sym_results;
        
        public Sym_result_storage(string data_storage_file)
        {
            this.data_storage_file = data_storage_file;

            builder = new StringBuilder();

            list_of_header_lines = new List<string>();
            list_of_sym_results = new List<string>();
        }

        public void create_file_header(List<double> DC_sizes, List<double> DC_shifts)
        {
            for(int i = 0; i < DC_sizes.Count(); i++)
            {
                if(i == 0)
                    builder.Append(DC_sizes[i].ToString());
                else
                    builder.Append(',' + DC_sizes[i].ToString());
            }
            list_of_header_lines.Add(builder.ToString());
            builder.Clear();

            for(int i = 0; i < DC_shifts.Count(); i++)
            {
                if(i == 0)
                    builder.Append(DC_shifts[i].ToString());
                else
                    builder.Append(',' + DC_shifts[i].ToString());
            }
            list_of_header_lines.Add(builder.ToString());
            builder.Clear();

            builder.Append("sort_size")
                    .Append(", sort_shift")
                    .Append(", DC_size")
                    .Append(", sort_shift")
                    .Append("ride_number");
            list_of_header_lines.Add(builder.ToString());
            builder.Clear();
        }

        public void add_data_to_storage(double sort_size, double sort_shift, double DC_size, double DC_shift, int ride_number)
        {
            builder.Append(sort_size.ToString())
                    .Append("," + sort_shift.ToString())
                    .Append("," + DC_size.ToString())
                    .Append("," + DC_shift.ToString())
                    .Append("," + ride_number.ToString());
            list_of_sym_results.Add(builder.ToString()); 
            builder.Clear();   
        }

        public void save_stored_data()
        {   
            list_of_header_lines.AddRange(list_of_sym_results);
            File.WriteAllLines(data_storage_file, list_of_header_lines);
        }
    }
}